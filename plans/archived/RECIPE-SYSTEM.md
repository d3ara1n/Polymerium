# Recipe：可复用的包预制清单

> 制定日期：2026-06-30（替换 2026-06-29 旧构想，旧构想全部作废）
> 关联：[POLY-120](https://d3ara1n.atlassian.net/browse/POLY-120)、GitHub #73
> 现状：5 个前置任务（POLY-115/116/117/118/119）已全部落地，本任务直接开工。

## 0. 一句话定义

Recipe = 命名的、可复用的浮动包引用清单。"批量导入收藏的包"。实例引入 recipe = 把清单**解析并快照**进 `Setup.Packages`（`Source = recipe://<id>`），此后实例与 recipe 各自独立演化——和整合包同为"引入非引用"，但无副作用、一个实例可引入多个。

非目标：recipe 在线分享平台；recipe 版本化；optional/条件条目；recipe 嵌套；recipe 编辑后联动已引入实例。

## 1. 关键事实（已对照代码核实，不要再调研）

- `Profile.Rice.Entry`（`submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/Profile.cs:47`）字段：`Pref` / `Enabled` / `Source`（null=手动）/ `Tags`。**模型零改动**。
- `PackageSourceHelper.Classify`（`src/Polymerium.Avalonia/Utilities/PackageSourceHelper.cs:35`）已识别 `recipe://` 前缀为 `Kind.Recipe`；构造用 `InternalUriHelper.Recipe(id)`。分组、部署优先级（手动 > recipe > 整合包）均已就位。
- **唯一的洞**：`InstanceSetupPageModel.GroupModelOf`（1656 行）对 Recipe 分支抛 `NotImplementedException`——recipe 包进实例后打开 Setup 页会崩，`RecipeGroupModel` 必须与机制同期落地。
- 浮动 pref：`pref://<repository>/<identity>` 不带 `@vid`；固定 = 加 `@versionId`。构造/解析走 `PackageHelper`。
- 浮动→固定解析：`RepositoryAgent.ResolveBatchAsync(IEnumerable<ScopedPackageIdentifier>, Filter)`（`IRepository.cs:28`），`Filter(Version, Loader, Kind)`。展示信息查询：`QueryBatchAsync` → `Project`。**不要用 `PackagePlanner`**——它是部署管线的，带 rule 语义还会丢展示信息。
- "添加包写 profile"范本：`PackageExplorerPageModel.CollectPendingAsync` + `InstanceSetupPageModel` 批量导入段（约 1148）：guard 内 `Packages.Add` + `persistenceService.AppendAction`。
- LockData 全自动派生（`SyncPackagesStage` 按 `(project, source)` diff），**本任务零 lock 代码**。
- Nanoid 已是 `TridentCore.Core` 传递依赖（v3.1.0），recipe id 用 `Nanoid.Generate(size: 12)`，免 slug 命名/冲突问题。

## 2. 数据模型（PersistenceService 嵌套类，FreeSql）

两张表，照 `FavoriteProject` 范式，定义在 `PersistenceService.cs` 内：

```csharp
public class Recipe
{
    [Column(IsPrimary = true)] public required string Id { get; set; } // nanoid(12)
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class RecipeItem // 即 Project，无版本概念；pref 由 Label/Namespace/ProjectId 现场构造，不存冗余字符串
{
    [Column(IsPrimary = true)] public required string Id { get; set; } // nanoid，recipe 内标识用
    public required string RecipeId { get; set; } // 外键 → Recipe.Id
    public required string Label { get; set; }
    public string? Namespace { get; set; }
    public required string ProjectId { get; set; }
    [Column(DbType = "BLOB")] public IList<string>? Tags { get; set; } // JSON；recipe 自身没有 Tags
    public string? Note { get; set; }
}
```

**Recipe 没有 Tags 字段**——tags 是包的属性，引入实例时衔接 `Entry.Tags`。

## 3. 机制层（无独立 RecipeService）

PersistenceService 自己就是 CRUD，不再包一层。新增的只有两个计算点：

### 3a. 引用清单 `GetRefList(recipeId) → IReadOnlyList<string>`

扫全部实例 profile 的 `Setup.Packages`，返回含 `Source == recipe://<id>` entry 的 profile key 列表。放在 Recipe 加载处（UI 阶段由 PageModel 调用；机制阶段随模型一并实现为静态 helper 或挂 PersistenceService 扩展——实现时定，就导航实例目录读 profile.json 一层事）。

- **删除保护**：ref list 非空拒删；UI 右键展示引用实例（同一次查询复用，不查两次）。
- 游离兜底：recipe 真没了而实例仍有 `recipe://` Source，组照常能用，组头降级显示 uri 原文 + 提示文本。
- **解散组（把组内包降级为手动包）不做**，后续单独立项。

### 3b. 引入逻辑 `ApplyRecipe` —— 直接写在 `InstanceSetupPageModel`

打开 dialog 选 recipe → 从持久层取完整 recipe（含 items）→ 原地写入。不抽服务、不进 Trident。

```
1. 取 recipe + items；items 由 (Label, Namespace, ProjectId) 构造 ScopedPackageIdentifier（无 vid）
2. RepositoryAgent.ResolveBatchAsync(items, new Filter(setup.Version, loaderId, null))
   - 成功 → 固定 pref（PackageHelper，带解析出的 vid）
   - 失败 → 原样浮动 pref + Enabled = false（留在列表，不阻断、不报失败清单）
3. 逐个构造 Profile.Rice.Entry {
     Pref, Enabled = 解析成败, Source = InternalUriHelper.Recipe(id),
     Tags = item.Tags 与已有同名包的 Tags 集合差（union - 已有），防刷重
   } → guard.Value.Setup.Packages.Add → AppendAction
```

同名包冲突不处理，部署优先级（手动 > recipe > 整合包）仲裁已落地。

## 4. RecipeGroupModel（机制的另一半，必须同期）

新增 `Models/RecipeGroupModel.cs`（继承 `GroupModelBase`，持 recipeId + 名称摘要 + recipe 是否仍在库），填 `GroupModelOf` 的 Recipe 分支。期外：组头/详情 modal 后续再说。组级启用/禁用/移除沿用 InstanceSetup 现有批操作，不为 recipe 特制。

## 5. UI（本阶段不做，仅锁定设计防返工）

- **`RecipesPage`**：一级页面（与实例无关），MainWindow 侧边栏 `NavigateCommand + x:Type` 挂入。卡片网格陈列（照 `InstancesPage` 范式）：名称/描述摘要/条目数；卡片角标菜单：导出、删除（ref 保护）。顶栏：新建、导入。
- **`RecipePage`**：一级详情页，`Navigate<RecipePage>(id)`。单页即编辑（不画只读/编辑两态）：头 = 名称/描述行内改 + 导出；主体 = item 列表。
  - item 行显示**解析后的 Project 信息**（`QueryBatchAsync`），不是 pref 裸串。
  - 异步加载模式照 InstanceSetupPage 的 diff 流派：`RecipeItemModel.Info` nullable + DynamicData filter 收裸 item → 批量 `QueryBatchAsync` 补齐。打开页 = 全量一把；添加 = filter 网住新增单元素解析。DB 实体无 profile 式实时事件，解析只在页面生命周期动作点触发。
- **添加 item**：开发期 input dialog 填 pref；上线前换 PackageExplorer 搜索接入（`RecipeItem` 按 project 存就是为了届时无损衔接）。
- **实例侧入口**：`InstanceSetupPage` SplitButton Flyout 加"从 Recipe 导入"（`Resources.resx` 三件套同步加 key）→ dialog 选 recipe → 走 §3b。

## 6. 实施顺序

1. 两个实体建模 + RefList helper
2. `RecipeGroupModel` + `GroupModelOf` 填洞
3. `InstanceSetupPageModel.ApplyRecipe`（含浮/固写入与 Tags 差集）
4. UI（RecipesPage → RecipePage → 实例侧入口 dialog），编辑页添加条目开发期 input dialog，上线前替换 explorer 搜索

验收：引入 recipe 后 Setup 页不崩且成组；解析失败条目以禁用浮动形态留列表；ref 非空拒删；recipe 删除后实例外照常用、组头降级。
