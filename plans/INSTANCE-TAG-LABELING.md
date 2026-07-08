# 实例标签管理 UI

> 制定日期：2026-07-08
> 定位：Instance 管理的补完任务。Phase C 已实现标签展示与筛选消费，缺用户贴/改/删标签的交互入口。
> 当前状态：草案
> 关联：[POLY-23](https://d3ara1n.atlassian.net/browse/POLY-23)（原始 Instance 管理 issue）

## 背景与动机

`InstancesPage` 的卡片（`InstanceCard`）和筛选 flyout 已消费标签数据（`Tags` 展示为色点药丸、flyout 支持按标签筛选），但标签的**写入**没有入口——用户无法贴/改/删标签。

现状：

- `InstancesPageModel` 的 `InstanceCardModel` 已从 `PersistenceService` 按 key 读 `string[]` 标签
- 筛选 flyout 的「我的标签」区已消费这些标签做多选筛选
- 色点颜色已按标签名 hash 取主题色板
- **缺贴标入口**，用户想改标签无处下手

## 目标

1. 提供贴标 / 管理标签的 UI 入口（右键菜单 + 独立编辑对话框/面板）。
2. 支持为单个实例增删标签（自由文本输入，非预定义集合）。
3. 标签变更实时反映到 `InstanceCard` 展示和 flyout 筛选。

## 非目标

- 不做全局标签管理页面（标签是实例属性，随实例生灭）。
- 不做标签自动补全/预定义列表（运行时扫所有实例标签去重聚合可供参考，但非强制）。
- 不改标签持久化机制（复用现有 `PersistenceService` 按 key 存 `string[]`）。

## 核心设计

### 贴标入口

右键菜单（三处同步：主界面侧边栏 / InstancesPage / InstancePage）增加「编辑标签…」项，点击弹出标签编辑弹窗。

### 标签编辑弹窗

参照 `Filters/CreateTagModal` 或 `Dialogs/` 的模态对话框范式：

```
┌─ 编辑实例标签 ──────────────────────── ✕ ─┐
│                                            │
│  实例：ATM9                                │
│                                            │
│  标签                                       │
│  ┌─────────────────────────────────────┐   │
│  │ [✕] 联机   [✕] 测试   [✕] 已弃坑   │   │
│  │ <输入新标签…>           [+ 添加]    │   │
│  └─────────────────────────────────────┘   │
│                                            │
│  建议标签（来自已有实例的标签去重）            │
│  [性能测试] [空岛] [RPG]                    │
│                                            │
│                    [取消]  [保存]           │
└────────────────────────────────────────────┘
```

- 已有标签显示为可移除的药丸（`[✕]`）。
- 自由文本输入 + 添加按钮新增标签。
- 「建议标签」区域展示全局标签聚合（运行时扫所有实例的 tags 去重），点击即添加。
- 保存时写 `PersistenceService` 按 key 存 `string[]`，`InstancesPageModel` 刷新对应 `InstanceCardModel.Tags`。

### 交互细节

- 右键菜单「编辑标签…」项统一由 `InstanceService` 暴露 `EditTagsAsync(key)` 方法，三处 ViewModel 转发。
- 编辑弹窗复用 `overlayService.CreateDialogAsync<TagEditorDialog>(key)` 模式。
- 保存后 `InstanceService` 触发 Pinned 可观察集合的侧效应无需动（标签不涉 pinned），但需通知 `InstancesPageModel` 刷新卡片——通过 `PersistenceService` 自身的事件或 `ProfileManager` 无关标签变动的方式。建议 `InstanceService` 新增 `TagsChanged` 事件或 `InstanceService.OnTagsChanged`，由 `InstancesPageModel` 订阅。

## 改动面

| 文件 | 改动 |
|------|------|
| `Dialogs/TagEditorDialog.axaml(.cs)`（新增） | 标签编辑弹窗视图 |
| `DialogModels/TagEditorDialogModel.cs`（新增） | 编辑逻辑：加载/保存/建议标签聚合 |
| `Services/InstanceService.cs` | 新增 `EditTagsAsync(string key)` 编排 + `TagsChanged` 事件 |
| `PageModels/InstancesPageModel.cs` | 订阅 `TagsChanged` 刷新卡片的 Tags |
| `MainWindowContext.cs` | 右键菜单新增「编辑标签…」项转发 InstanceService |
| `PageModels/InstancesPageModel.cs` | 同上 |
| `PageModels/InstancePageModel.cs` | 同上 |
| `Pages/InstanceSetupPage.axaml` / `Pages/InstancePage.axaml` / `MainWindow.axaml` | 右键菜单加「编辑标签…」项 |
| `Properties/Resources.{resx,zh-hans.resx,Designer.cs}` | 新增标签相关 key |

## 验收标准

| 场景 | 期望 |
|------|------|
| 右键实例 → 编辑标签 | 弹出弹窗，显示当前标签和全局建议标签 |
| 添加标签 | 输入文本 → 添加 → 卡片色点药丸出现，flyout 筛选出现该标签 |
| 移除标签 | 点药丸 ✕ → 卡片更新，flyout 筛选消失 |
| 建议标签点击 | 点击建议标签 → 添加为实例标签 |
| 多实例标签独立 | 各实例标签各自持久化，不串扰 |
| 标签在筛选 flyout 中实时反映 | 增删标签后 flyout 的标签多选区刷新 |

## 备选方案备案

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 实例属性页内嵌标签编辑 | 放在现有 InstancePropertiesPage 的字段区 | 属性页已过载，标签是管理属性不是技术属性；右键直达更快 |
| 内联编辑（卡片上直接改） | 点标签区变输入框 | 触屏/鼠标操作复杂，多个标签时交互混乱 |
| 标签预定义池 | 用户从预设列表选 | 标签是用户自由分类，预定义与"主观用途分类"理念冲突 |
