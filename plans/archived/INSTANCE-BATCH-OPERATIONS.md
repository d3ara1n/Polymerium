# InstancesPage 批量操作

> 制定日期：2026-07-08
> 定位：Instance 管理的补完任务。Phase C 只实现了单实例右键操作，缺多选后的批量操作能力。
> 当前状态：✅ 已实施（不作）
> 关联：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)

## 背景与动机

`InstancesPage` 当前是纯管理视图，卡片网格 + 右键菜单 + 搜索/筛选。所有操作（导出/删除/部署/打开文件夹/设置/属性）都是**单项**的。

用户场景：整理实例列表时想批量删除不再需要的测试实例、批量导出要分享的整合包、批量部署升级——这些都需要多选 + 批量操作。

现状：

- `InstanceCard` 是零按钮卡片，整卡点击导航进实例页
- 右键菜单有全套动作（启动/部署/导出/文件夹/设置/属性），但都是单项
- `InstanceService` 已聚合单实例动作
- 无多选模式、无批量操作入口

## 目标

1. 进入多选模式（勾选或按 Ctrl/Shift 多选卡片）。
2. 选中后顶栏出现批量操作栏（不遮挡搜索/筛选）。
3. 支持批量：导出、删除、部署。
4. 批量操作走二次确认（高危操作弹确认对话框）。
5. 退出多选模式（清空选择或按 Esc）。

## 非目标

- 不做多实例同时启动（运行中状态冲突处理复杂，价值不高）。
- 不做多实例属性编辑（属性是单实例配置）。
- 不改 `InstanceCard` 在非多选模式的行为（整卡点击仍导航）。

## 核心设计

### 进入多选模式

两种入口：

1. **长按 / Ctrl+点击**单张卡片 → 卡片进入勾选态，顶栏出现批量操作栏
2. 不引入显式「选择」按钮（桌面用户直觉：Ctrl/Shift 多选）

卡片在 SelectionMode 启用后左上角出现 `CheckBox`（与现有 📌 角标共存），视觉状态：

```
┌─────────────────────┐
│ ☑ 📌                │   ← 勾选 + pinned 共存
│                     │
│  ATM9               │
│  CurseForge         │
│  Fabric · 1.20.1    │
│  2h ago             │
└─────────────────────┘
```

支持 Shift 范围选择（首尾之间的卡片全选）、Ctrl 增减单卡。

### 批量操作栏

选中的卡片数量 > 0 时，顶栏搜索/排序按钮区下方（或替换部分）出现浮动操作栏：

```
┌─ 已选 3 个实例 ───────────────────────────── [取消选择] ─┐
│ [⬇ 导出]  [🗑 删除]  [▶ 部署]                           │
└──────────────────────────────────────────────────────────┘
```

- 操作按钮根据选中实例的状态启用/禁用（如运行中的实例不可删除/部署）。
- 「取消选择」退出多选模式，所有卡片恢复非选择态。
- 操作栏不遮挡搜索框（置于搜索框下方或与同排紧凑布局）。

### 批量操作实现

各操作复用 `InstanceService` 的单实例方法，批量调：

```csharp
// InstanceService 新增
public async Task BatchExportAsync(IEnumerable<string> keys) { ... }
public async Task BatchDeleteAsync(IEnumerable<string> keys) { ... }
public async Task BatchDeployAsync(IEnumerable<string> keys) { ... }
```

逐实例遍历，失败隔离（一个失败不影响其他的）。

**批量删除**——走二次确认：

```
删除 3 个实例？
以下实例将被永久删除（快照保留）：
  ☐ ATM9          Fabric 1.20.1
  ☐ Skyblock      Quilt 1.21
  ☐ Test World    Forge 1.19.2

[取消]  [我已了解，删除]
```

列出实例名 + 关键属性，勾选确认每个。

**批量导出**——逐个弹出保存对话框（或一次选择文件夹目录，全部导出到该目录），复用 `InstanceService.ExportInstanceAsync`。

**批量部署**——逐个触发 `InstanceService.DeployAsync`，失败汇总到通知。

### ViewModel 状态

```csharp
// InstancesPageModel 新增
[ObservableProperty] public partial bool IsMultiSelectMode { get; set; }
[ObservableProperty] public partial int SelectedCount { get; set; }
public IReadOnlyList<string> SelectedKeys { get; set; } = [];

partial void OnIsMultiSelectModeChanged(bool value)
{
    if (!value) SelectedKeys = [];  // 退出模式清空
}
```

## 方案

### 决策：不作

实例的创建和删除本身就是需要严肃对待的高成本操作——删除走层层验证、实例数据量大恢复困难，批量操作更适合文件管理器式的轻量资源管理，与实例管理的属性不匹配。导出/部署也各需用户确认路径和参数，批量反而增加误操作风险。
