# 包列表分组 UI：实施影响面与故障定位地图

> 制定日期：2026-07-07
> 定位：POLY-119（实例包列表按来源分组）实施后的**影响面与故障定位地图**。面向"分组不出现 / 折叠不生效 / 组头信息不加载 / 排序错 / 搜索计数错"等问题的快速定位。施工蓝本见 `plans/PACKAGE-GROUPING-UI.md`（已实施，以第四轮校准为准）。
> 关联：[POLY-119](https://d3ara1n.atlassian.net/browse/POLY-119)；依赖 [POLY-118](SOURCE-REFERENCE-SEMANTICS)（`PackageSourceHelper`：分组依据）、[POLY-116](DeploymentPriority.md)（`SourceOrders`：组间顺序）。
> 当前状态：实施完成（2026-07-07）

---

## 0. 核心变化（定位问题的锚点）

1. **列表数据模型变了** —— 旧 `StageView`（扁平 `ReadOnlyObservableCollection<InstancePackageModel>`）被 `FlatView`（`ReadOnlyObservableCollection<PackageListItemBase>`）取代。`PackageListItemBase` 多态：`Header`（渲染组）+ `Entry(Package)`（渲染包），都持有同一个 `GroupModelBase` 实例。**任何"列表项相关"问题先想：FlatView 里是 Header 还是 Entry。**
2. **管道是双流反应式推导，不是显式 add** —— `entries`（filtered→Transform→Entry）与 `headers`（filtered→去散装→GroupOn→Header）两条独立流，无键 `Or` 合并。Header 不是被 add 的，是"该组有包通过过滤"的反应式产物。`_stageSource` 一变，管道自动推到 FlatView。
3. **GroupModel 既是分组依据又是信息载体** —— 同一组的 Header 和所有 Entry 引用**同一个** `GroupModelBase` 实例（经 `_groupModels` 字典 + `GroupModelOf`/`EnsureModpackGroup` 去重）。折叠能整组生效全靠这个共享引用。
4. **项的"出现"与"信息填充"解耦** —— Merge 同步 diff 后，项骨架立刻进 FlatView（管道秒级）；包的 `Info` 和组的 `Name`/`Thumbnail` 由两个异步任务（`RefreshPackagesAsync` / `RefreshGroupsAsync`）慢慢灌。
5. **折叠靠 `AutoRefreshOnObservable`，无 Subject** —— 管道订阅每个 item 的 `Group.IsExpanded`（GroupModelBase 是 INPC），翻转即重判折叠 filter，无需手动 signal。

---

## 1. 责任域地图（按文件）

| 责任域 | 文件 | 职责 | 出错查这里 |
|--------|------|------|-----------|
| **列表项模型** | `Models/PackageListItemBase.cs` | 多态 `Header`/`Entry`，持有 `Group` | 项类型/Group 引用 |
| **组模型** | `Models/GroupModelBase.cs`、`ModpackGroupModel.cs`、`LooseGroupModel.cs` | 分组依据 + 信息载体；Modpack 的 `Name`/`Thumbnail`/`IsInfoLoaded` | 组信息字段、`RequireGuideLine`（导引线显隐） |
| **模板选择器** | `Controls/PackageListItemTemplateSelector.cs` | `IDataTemplate`：Header→HeaderTemplate、Entry→EntryTemplate（容器的 DataContext 永远是 item 本身） | 项渲染成什么模板 |
| **排序** | `Utilities/PackageListItemComparer.cs` | `(SourceOrders 档位, Header<Entry, PersistentIndex)`，散装恒末 | 组顺序/头身顺序错 |
| **管道+命令+加载** | `PageModels/InstanceSetupPageModel.cs` | 双流管道、`GroupModelOf`/`EnsureModpackGroup`、`GetGroupKey`、`RefreshGroupsAsync`、`ToggleGroupExpanded`、`ViewGroupDetailsAsync`、`FilteredPackageCount` | 见下表症状→入口 |
| **视图** | `Pages/InstanceSetupPage.axaml` | `PackageListItemTemplate` 资源、`ModpackGroupContentTemplate`、Header/Entry 模板、单 `ItemsControl`+`VirtualizingStackPanel` | 模板绑定、组头视觉 |
| **分组依据** | `Utilities/PackageSourceHelper.cs`（POLY-118） | `Classify`/`Kind`/`CanUpdate`/`CanRemove`/`CanUngroup` | 某包该归哪组 |
| **组顺序数据** | `submodules/Trident.Net/.../FileModels/Profile.cs`（`Rice.SourceOrders`，POLY-116） | 组间排序的覆盖顺序 | 组排错了 |

---

## 2. 排错入口（症状 → 查哪里）

| 症状 | 查 |
|------|-----|
| **组头不出现 / 某组整组不显示** | headers 分支：`filtered.RemoveKey().Filter(pkg => pkg.Entry.Source is not null).GroupOn(GetGroupKey)`；`GetGroupKey` 是否把该包算进对应组；`PackageSourceHelper.Classify` |
| **折叠点了没反应 / 只折叠了一部分** | `AutoRefreshOnObservable(item => item.Group.WhenPropertyChanged(g => g.IsExpanded))`；`ToggleGroupExpanded` 是否翻转了 `IsExpanded`；**关键**：Header 和它的 Entry 是否拿到**同一个** GroupModel（`GroupModelOf`/`EnsureModpackGroup` 必须返回同一实例——查 `_groupModels` 字典 key） |
| **组头图标/名字不加载（一直占位）** | `RefreshGroupsAsync`：是否被 `TriggerPackageMerge` 调起；`EnsureModpackGroup` 建的实例和管道用的是否同一；`IsInfoLoaded` 是否被错误前置；`PackageHelper.TryParse` 是否解析成功；`dataService.ResolvePackageAsync(Kind=Modpack)` 网络/数据 |
| **组顺序错 / 头不在身前** | `PackageListItemComparer`：`CompareGroup`（`SourceOrders.IndexOf`、散装恒末、Recipe<Modpack 默认档位）、`RankOf`（Header=0<Entry=1）、`IntraIndexOf`（`PersistentIndex`） |
| **搜索计数器数字不对** | 计数器绑 `FilteredPackageCount`（不是 `FlatView.Count`——后者含 Header）。`FilteredPackageCount` 由 `filtered.QueryWhenChanged(items => items.Count)` 订阅，**只数包、不含 Header、不受折叠影响** |
| **散装包没沉底 / 画了导引线** | 散装应归 `LooseGroupModel`（`RequireGuideLine=false`）；`CompareGroup` 里 `is LooseGroupModel` 判定恒末 |
| **加/删散装包列表不更新** | entries 分支用 `filtered.Transform`（动态跟踪）；散装走 entries（无 Header）。若不更新，查 `filtered`/`_stageSource` 变更是否流入 |
| **整合包组里多了/少了不该有的包** | 组的成员由**外部引用**（整合包）决定，不能从包列表增删（`CanRemove` 在组里恒 false）。成员错 → 查 profile 里该 source 的 Entry 集合，不是查 UI |

---

## 3. 数据流时间线（一次打开实例）

| 时刻 | FlatView | 项内容 |
|------|----------|--------|
| `TriggerPackageMerge` 同步 diff 完成 | Header + Entry 骨架都进来（管道秒级反应） | 包 `Info` 还 null、组 `Name`/`Thumbnail` 还 null → 占位/FallbackValue |
| `RefreshPackagesAsync` 异步回来 | （项不变） | 各 Entry 包 `Info` 填上 |
| `RefreshGroupsAsync` 异步回来 | （项不变） | 各 Header 组 `Name`/`Thumbnail` 填上 |

**项的增删由管道同步推导（取决于包在不在、过不过滤）；项的详细信息由 Merge 阶段起的两个异步任务灌。** Header 和 Entry 同时进列表，只是内容来源不同（组信息走 `RefreshGroupsAsync`，包信息走 `RefreshPackagesAsync`）。

---

## 4. 未做 / 留待后续

- **组级「更多」菜单**（整组启用/禁用、解散）：未实现，命令（`ToggleGroupEnabled`/`RemoveGroup`）未加。
- **导引线交互**（hover 高亮、点击定位）：只做视觉，交互标 TODO。
- **按来源分色的导引线**：当前单色（`ControlSecondaryForegroundBrush`）。
- **`RecipeGroupModel`**：Recipe 系统未落地，`GroupModelOf` 对 recipe 命中抛 `NotImplementedException`（当前数据无 `recipe://` source，不可达）。
- **组拖拽排序 / 展开态持久化**：未做。
