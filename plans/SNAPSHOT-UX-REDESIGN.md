# 快照管理界面重构设计

> 状态：**设计评审中**，未动代码。本文用于对齐方向与细节，评审通过后再分阶段实现。

## 背景

当前快照功能由一个 `660×480` 的 `SnapshotsModal` 承载，内部 `<husk:Frame>` 三级导航：

```
SnapshotsModal (660×480)
└─ Frame
   ├─ SnapshotPortalPage     ← 入口：两张大卡片「创建 / 列表」二选一
   ├─ SnapshotCreationPage   ← 创建：左填标签备注，右「拍摄」→「创建入库」两段式
   └─ SnapshotManagementPage ← 管理：左列表(40%) + GridSplitter + 右详情(60%)
```

从人机交互角度看，主要问题集中在：流程割裂、空间局促、实现细节（拍摄/入库两阶段）泄露到 UI、Diff 价值未兑现、列表缺可扫描性、危险操作语义弱。本文给出系统性改造方案。

## 目标与非目标

**目标**

1. 把「创建」与「管理」收敛为同一个时间线工作台，减少无谓跳转。
2. 让快照列表具备「版本演进」的时间线心智，可扫描、可定位。
3. 兑现 Diff 的核心价值：任意两点对比 + 增删明细可展开。
4. 把创建动作折叠为单一用户操作（内部仍两阶段）。
5. 危险操作（还原/删除）有明确语义与代价预览。

**非目标（本次不做）**

- 自动/定时快照（需事件触发机制，属新增能力，单独立项）。
- 跨实例快照、快照导出/导入。
- 底层存储格式变更。

## 能力边界（决定方案可行性）

`SnapshotManager.InstanceSnapshots`（scoped handle）现有方法：

| 方法 | 用途 | 方案依赖 |
|---|---|---|
| `List()` | 全部快照（按时间倒序） | 列表 |
| `Get(id)` / `TryGet(id, out)` | 单个快照 | 详情 |
| `GetReferences(snapshotId)` | 快照的全部文件引用（`RelativePath/Hash/Size/...`） | **Diff 明细** |
| `Delete(id)` | 删除快照 | 删除 |
| `TakeAsync(collected, processed, token)` | 阶段1：扫描文件、算哈希 | 创建 |
| `CommitAsync(snapshot, references, copied, token)` | 阶段2：拷贝 objects + 写库 | 创建 |
| `RestoreAsync(snapshotId, restored, token)` | 还原**任意**快照 | 还原 |

`ISnapshotStore` 另有（handle 未暴露）：

| 方法 | 用途 | 方案依赖 |
|---|---|---|
| `GetAllReferencedHashes()` | 所有被引用 hash 集合 | 存储占用 |
| `DeleteOrphanReferences()` | 删除孤立引用 | 清理入口 |

**结论**：方案 A–E、以及 F 的存储占用/还原预览均可落地；清理入口需在 handle 上加一个 `CleanupOrphans()` 薄封装；「当前」徽章需额外的 current snapshot id 追踪（见开放问题）。

---

## 总体方案：单一时间线工作台

### 导航流变化

```
改造前:  Modal → Portal(二选一) → CreationPage / ManagementPage
改造后:  Modal/Sidebar → ManagementPage(时间线工作台)
                        ├─ 顶部常驻 [+ 保存当前状态]
                        ├─ 列表项勾选两个 → 右栏对比视图
                        └─ 右栏：详情 / 创建面板 / 对比视图（三态切换）
```

`SnapshotPortalPage` 与 `SnapshotCreationPage` 作为独立页面**移除**：创建逻辑内联进管理页右栏（或抽成可复用 `Component`），Portal 的二选一价值被常驻创建按钮取代。

### 容器选择（二选一，待定）

| 方案 | 优点 | 缺点 |
|---|---|---|
| **放大 Modal**（如 `960×640`） | 改动最小，保留现有 `SnapshotsModal` 结构 | Modal 本质是打断式，详情/Diff 仍受限于固定尺寸 |
| **改为 Sidebar**（从右侧滑入，宽 `~520`+） | 非打断、实例级操作语义更贴；可铺满高度获得更多纵向空间；项目已有 `Sidebars/` 体系 | 需把 `SnapshotsModal` 迁到 `Sidebars/SnapshotsSidebar`，导航由 `<husk:Frame>` 保留 |

**建议**：若想最小化改动先选「放大 Modal」；若追求长期体验一致选「Sidebar」。下文布局以「放大 Modal」为基线描述，Sidebar 版同理。

---

## 详细设计

### A. 信息架构：砍掉 Portal

- `SnapshotPortalPage(.axaml/.cs)` + `SnapshotPortalPageModel.cs` **删除**。
- `SnapshotsModalModel.OnInitializeAsync` 里 `_navigateHandler` 初始目标改为 `SnapshotManagementPage`。
- 顶部标题区由 Modal 头部承担（已存在），Portal 里的说明文字移到管理页空状态/帮助提示。

### B. 管理页布局重构

整体线框（放大到 `960×640` 后）：

```
┌─ 快照 · <实例名> ──────────────────────────────────────────── ✕ ─┐
│ ┌ 工具栏 ───────────────────────────────────────────────────┐  │
│ │ [+ 保存当前状态]   排序▾   筛选▾      共 12 个 · 3.2 GB  ⚙清理│  │
│ └───────────────────────────────────────────────────────────┘  │
│ ┌ 左：时间线列表 ─────────┐ ┌ 右：详情/创建/对比（三态）─────┐ │
│ │ ● v3   ◀ 当前           │ │  部署到 1.20.1         [对比 ▾] │ │
│ │ │  1.8GB · +2 mods ↑   │ │  2026-06-25 · 手动              │ │
│ │ ● v2                    │ │  ─────────────────────────────  │ │
│ │ │  1.7GB · -14 文件 ↓  │ │  备注：Fabric→Quilt ...         │ │
│ │ ● v1 (初始)             │ │  [包48][文件1.2k][1.8GB]        │ │
│ │    1.5GB · 起点         │ │  与 v2 的变化    [展开明细 ▾]   │ │
│ │                         │ │   +12 文件 -14 +2 包            │ │
│ │ (空: 大按钮创建首个)    │ │  游戏 1.20.1 · Quilt 0.26       │ │
│ └─────────────────────────┘ │  ─────────────────────────────  │ │
│                             │  [⤺ 还原]          [🗑 删除]    │ │
│                             └─────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

示意 axaml 骨架（贴合 Huskui 规范，仅示结构，非最终代码）：

```xml
<husk:Page x:Class="...SnapshotManagementPage"
           Header="{x:Static lang:Resources.SnapshotManagementPage_Title}"
           ScrollViewer.VerticalScrollBarVisibility="Disabled"
           x:DataType="app:SnapshotManagementPageModel">
  <DockPanel>
    <!-- 工具栏 -->
    <Grid DockPanel.Dock="Top" Margin="0,0,0,8"
          ColumnDefinitions="Auto,Auto,*,Auto">
      <Button Grid.Column="0" Classes="Primary"
              Command="{Binding SaveCurrentCommand}">
        <husk:IconLabel Icon="SaveSync"
          Text="{x:Static lang:Resources.SnapshotManagementPage_SaveCurrentButtonText}"/>
      </Button>
      <!-- 排序/筛选下拉略 -->
      <StackPanel Grid.Column="3" Orientation="Horizontal" Spacing="8">
        <TextBlock Foreground="{StaticResource ControlSecondaryForegroundBrush}">
          <Run Text="{Binding SnapshotCount}"/>
          <Run Text="{x:Static lang:Resources.SnapshotManagementPage_CountSuffixText}"/>
          <Run Text="·"/>
          <Run Text="{Binding TotalSizeFormatted}"/>
        </TextBlock>
        <Button Classes="Small" Command="{Binding CleanupCommand}">
          <husk:IconLabel Icon="Broom" Text="..."/>
        </Button>
      </StackPanel>
    </Grid>

    <Grid ColumnDefinitions="Min(360,*) ,Auto,*">
      <!-- 左：时间线列表 -->
      <local:SnapshotTimeline .../>
      <GridSplitter Grid.Column="1" .../>
      <!-- 右：三态切换 -->
      <husk:SwitchContainer Grid.Column="2" TargetType="x:String"
            Value="{Binding RightPaneMode}">
        <husk:SwitchCase Value="Detail">  ...详情... </husk:SwitchCase>
        <husk:SwitchCase Value="Create">  ...创建面板... </husk:SwitchCase>
        <husk:SwitchCase Value="Compare"> ...对比视图... </husk:SwitchCase>
      </husk:SwitchContainer>
    </Grid>
  </DockPanel>
</husk:Page>
```

> 说明：右侧三态用 `husk:SwitchContainer`（项目已用于创建页的拍摄/提交切换），比写多个 `IsVisible` 更清晰。

### C. 时间线列表项增强

列表项从「图标+标签+时间」升级为带竖线节点 + 大小 + 变化指示器的信息密度节点：

```
    ●  部署到 1.20.1              ◀ 当前
    │  2026-06-25 · 1.8GB · +2 mods ↑(绿)
    ●  版本 v2
    │  2026-06-20 · 1.7GB · -14 文件 ↓(红)
    ●  版本 v1（初始快照）
       2026-06-01 · 1.5GB · 起点
```

变化指示器直接复用现有 `ComputeDiff`（当前快照 vs `Previous`），无需新接口：

```xml
<!-- ItemTemplate 片段 -->
<Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
  <StackPanel Width="20">
    <Ellipse Width="10" Height="10" Fill="{StaticResource ControlAccentForegroundBrush}"/>
    <Rectangle Width="2" Height="28" Fill="..."/>
  </StackPanel>
  <StackPanel Grid.Column="1" Spacing="2">
    <StackPanel Orientation="Horizontal" Spacing="6">
      <TextBlock FontWeight="Strong" Text="{Binding DisplayLabel}"/>
      <!-- 当前徽章：需 current id 支持 -->
      <Border Classes="AccentTag" IsVisible="{Binding IsCurrent}"
              Padding="6,1" CornerRadius="...">
        <TextBlock FontSize="Caption" Text="{x:Static ...CurrentBadgeText}"/>
      </Border>
    </StackPanel>
    <StackPanel Orientation="Horizontal" Spacing="6">
      <TextBlock FontSize="Small" Foreground="Secondary"
                 Text="{Binding CreatedAtText}"/>
      <TextBlock Text="·"/>
      <TextBlock FontSize="Small" Text="{Binding TotalSizeFormatted}"/>
      <TextBlock Text="·"/>
      <!-- 变化指示：绿增红减 -->
      <TextBlock FontSize="Small"
        Foreground="{Binding DeltaTone}"
        Text="{Binding DeltaText}"
        IsVisible="{Binding HasDelta}"/>
    </StackPanel>
  </StackPanel>
</Grid>
```

**需扩展 `SnapshotItemModel`**：新增 `IsCurrent`、`DeltaText`（如 `+2 mods` / `-14 文件`）、`HasDelta`、`DeltaTone`（绿/红 brush 或枚举）。`DeltaText` 由 `Previous` 差集计算（已有 `ComputeDiff` 逻辑下沉到 model 或在 ViewModel 构建时填充）。

### D. 任意两点对比 + 明细可展开

**交互**：列表项支持勾选（`SelectionMode` 改为 `Multiple,Toggle` 或在每行加 `CheckBox`）。选中两个时，右栏自动切到「对比视图」，并出现 `[对比] [取消]`。

**对比视图**：

```
   对比 v3 ◀ ── ▶ v1
   ─────────────────────────────
   文件   +128              -0
   包     +6 (OptiFine,…)   -0
   ─────────────────────────────
   [▶ 展开新增文件]   [▶ 展开移除文件]
```

明细数据来自 `GetReferences(id)` 的 `RelativePath` 差集，**纯前端**，无需改 handle。ViewModel 新增：

```csharp
[ObservableProperty] public partial SnapshotCompareModel? Compare { get; set; }
[ObservableProperty] public partial IReadOnlyList<SnapshotItemModel> SelectedForCompare { get; set; } = [];

partial void OnSelectedForCompareChanged(...) {
    RightPaneMode = SelectedForCompare.Count == 2 ? "Compare" : "Detail";
    if (SelectedForCompare.Count == 2)
        Compare = BuildCompare(SelectedForCompare[0], SelectedForCompare[1]);
}

private SnapshotCompareModel BuildCompare(SnapshotItemModel a, SnapshotItemModel b) {
    var ra = Context.Handle.GetReferences(a.Source.Id);
    var rb = Context.Handle.GetReferences(b.Source.Id);
    var pa = ra.Select(x => x.RelativePath).ToHashSet();
    var pb = rb.Select(x => x.RelativePath).ToHashSet();
    // 新增 = 在 b 不在 a；移除 = 在 a 不在 b；明细保留对应 ReferenceInfo
    ...
}
```

新增 `Models/SnapshotCompareModel.cs`（独立文件，符合「共享 model 独立成文」约定）：含 `Left/Right` 快照摘要、`FilesAdded/Removed`（含明细 `IReadOnlyList<ReferenceInfo>` 或其映射）、`PackagesAdded/Removed`。

### E. 创建合并为一步

把 `TakeAsync`（拍摄）+ `CommitAsync`（入库）对用户折叠成**一个动作「保存当前状态」**：

```
点击 [+ 保存当前状态]
  → 右栏切到「创建面板」（标签 + 备注 + [保存] [取消]）
  → 点击 [保存]
      内部: TakeAsync(进度) → 填 label/remark → CommitAsync(进度)
      UI  : 单一进度 "正在保存… 823/1024"
  → 完成后回到详情视图，并自动选中新建快照
```

`SnapshotCreationPage(.axaml/.cs)` + `SnapshotCreationPageModel.cs` 作为独立页面**移除**；其中「拍摄预览」（文件分区/分类卡片，`SnapshotTakenModel`）可作为**可选的保存后摘要**保留在详情或 toast，但不再是必经的两段式步骤。

ViewModel 状态机（在 `SnapshotManagementPageModel` 内）：

```
RightPaneMode: Detail (默认) ⇄ Create (点击保存按钮) ⇄ Compare (选中两个)

SaveCurrentCommand → RightPaneMode = "Create"
ConfirmSaveCommand (async):
   IsSaving = true
   var (snap, refs) = await Handle.TakeAsync(progress, token)   // 阶段1
   snap = snap with { Label = Label, Remark = Remark }          // 填用户输入
   await Handle.CommitAsync(snap, refs, progress, token)        // 阶段2
   _source.AddOrUpdate(new item...)                             // 刷新列表
   SelectedSnapshot = newItem
   RightPaneMode = "Detail"
   IsSaving = false
```

### F. 危险操作语义化 + 存储治理

**还原前代价预览**：点击「还原」时，确认对话框直接给出影响（而非仅确认"确定?"）：

```
还原「部署到 1.20.1」？
这将用该快照覆盖当前实例的托管文件：
  +128 个文件将被写入
  -64  个当前文件将被删除
  加载器：Fabric → Quilt（变更）
[取消]  [我已了解，还原]
```

代价计算：`diff(目标快照引用, 当前实例)`。当前实例文件的精确 diff 需重新扫描（重）；**近似方案**用「最近一次还原/创建的快照」作基准，退化为 `ComputeDiff(target, baseline)`。两种实现路径在开放问题中列出。

**危险色调**：还原按钮提升为 `Warning`（琥珀）而非 `Primary`，因为其后果（覆盖未快照工作）比删除一条历史更严重；删除保持 `Danger`。

**存储占用 + 清理**：工具栏常驻 `共 N 个 · X GB`。`X GB` 由 `GetAllReferencedHashes()` + objects 目录扫描累加。清理入口调用 handle 新增的 `CleanupOrphans()`：

```csharp
// InstanceSnapshots 上新增（薄封装 store.DeleteOrphanReferences + 扫 objects）
public CleanupResult CleanupOrphans() { ... }
```

> 现有 `plans/archived/GC-DESIGN.md` 已有 GC 设计基础，可对齐。

---

## 数据模型与 ViewModel 变更汇总

| 文件 | 变更 |
|---|---|
| `Models/SnapshotItemModel.cs` | 加 `IsCurrent`、`DeltaText`、`HasDelta`、`DeltaTone` |
| `Models/SnapshotCompareModel.cs` | **新增**（任意两点对比结果，含明细） |
| `Models/SnapshotRestoreImpactModel.cs` | **新增**（还原代价预览：增/删/加载器变更） |
| `PageModels/SnapshotManagementPageModel.cs` | 接管创建/对比逻辑；加 `RightPaneMode`、`SelectedForCompare`、`TotalSizeFormatted`、`CleanupCommand` |
| `Pages/SnapshotManagementPage.axaml` | 工具栏 + 三态右栏 + 时间线列表 |
| `Pages/SnapshotPortalPage.*` | **删除** |
| `Pages/SnapshotCreationPage.*` | **删除**（逻辑并入管理页） |
| `ModalModels/SnapshotsModalModel.cs` | 初始导航目标改为管理页 |
| `Modals/SnapshotsModal.axaml` | 尺寸放大（或迁 Sidebar） |
| `submodules/.../SnapshotManager.cs` | `InstanceSnapshots` 加 `CleanupOrphans()` |

## 本地化 key 清单（新增）

延续 `SnapshotManagementPage_` 前缀，需同步写入 `Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs`：

| Key | en | zh-Hans |
|---|---|---|
| `SnapshotManagementPage_SaveCurrentButtonText` | Save current state | 保存当前状态 |
| `SnapshotManagementPage_CountSuffixText` | snapshots | 个快照 |
| `SnapshotManagementPage_DeltaAddedText` | +{0} {1} added | +{0} {1} 新增 |
| `SnapshotManagementPage_DeltaRemovedText` | -{0} {1} removed | -{0} {1} 移除 |
| `SnapshotManagementPage_CurrentBadgeText` | Current | 当前 |
| `SnapshotManagementPage_CompareButtonText` | Compare | 对比 |
| `SnapshotManagementPage_CompareFromToText` | {0} → {1} | {0} → {1} |
| `SnapshotManagementPage_ExpandDetailsText` | Expand details | 展开明细 |
| `SnapshotManagementPage_RestoreImpactTitle` | Restore impact | 还原影响 |
| `SnapshotManagementPage_RestoreImpactFormat` | This will overwrite managed files of the current instance | 这将覆盖当前实例的托管文件 |
| `SnapshotManagementPage_CleanupButtonText` | Clean up orphaned data | 清理孤立数据 |
| `SnapshotManagementPage_EmptyCreateFirstText` | Create your first snapshot | 创建你的第一个快照 |

（沿用既有 key 的不在此列；Creation/Portal 页删除后，其专属 key 一并清理。）

---

## 迁移步骤（建议分四阶段）

1. **阶段 1 — 架构收敛**：删 Portal；管理页接管导航入口；Modal 放大（或迁 Sidebar）；创建按钮常驻。可发布，体验已显著改善。
2. **阶段 2 — 创建合并**：管理页内联创建面板；移除 `SnapshotCreationPage`；单一进度。
3. **阶段 3 — 时间线 + 列表增强**：列表项升级（竖线节点、大小、变化指示器、空状态引导）。
4. **阶段 4 — 对比 + 治理**：任意两点对比与明细；还原代价预览；存储占用与清理入口。

每阶段独立可交付、可评审。

## 开放问题与取舍

1. **容器：放大 Modal vs 迁 Sidebar** — 待定，影响阶段 1 工作量。
2. **「当前」徽章的数据来源** — handle 无 current snapshot id。三种路径：
   - (a) 还原/创建时在 `profile` 或 snapshot 表写一个 `IsCurrent` 标记（需后端改，最准）；
   - (b) 用 `profile.Setup` 与各快照 `Metadata` 做结构对比近似推断（前端可做，可能误判）；
   - (c) 本期不做徽章，仅保留大小/变化指示器。
   - 建议本期先 (c)，徽章随自动快照/状态追踪一起做。
3. **还原代价预览的精度** — 精确需重扫当前实例文件（耗时，与还原本身同级）；近似用 baseline 快照（瞬时，但反映的是「相对上次快照」而非「相对当前真实文件」）。建议默认近似，并在文案注明基准。
4. **创建时的「文件分区/分类」预览**（现有 `SnapshotTakenModel`）是否保留 — 建议降级为保存成功后的 toast/详情摘要，不作为必经步骤。
5. **多语言一致性** — 现有 `SnapshotCreationPage`/`Portal` 大量硬编码中文，重构时统一收口到 `Resources.*`（本次清单已覆盖）。
