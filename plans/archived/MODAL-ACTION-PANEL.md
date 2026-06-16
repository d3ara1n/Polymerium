# 模态框动作按钮的平台感知布局（ModalActionPanel）

## 背景

Huskui.Avalonia 与 Polymerium.Avalonia 的模态/对话框普遍采用「主按钮（确认）在左、其余（取消）在右」的布局。这在 Windows/Linux 上是常规约定，但在 macOS 上违反 Apple HIG——macOS 要求**默认动作（primary / `IsDefault`）位于最右侧**，取消位于其左侧。

根因两条：

1. Huskui 的 `Dialog` 控件主题把动作按钮写死在 `Grid ColumnDefinitions="*,*"` 里，Primary 恒为 `Grid.Column="0"`（左）。所有 `<husk:Dialog>` 都继承这个布局。
2. Polymerium 自己的 `<husk:Modal>` 里手写按钮 Grid / StackPanel，主按钮普遍在左。

Huskui.Avalonia 在本仓库以 **NuGet** 形式引用，是独立外部库，必须先在库内完成改造并发版，Polymerium 才能升级消费。

---

## 阶段一：Huskui.Avalonia —— ✅ 已完成（1.3.0）

### 新增控件 `ModalActionPanel`

`Huskui.Avalonia/Controls/ModalActionPanel.cs`，自写 `Panel`（参考库内 `FlexWrapPanel`），**不改写子级主题**——子按钮自带什么主题就是什么。

设计要点：**两件正交的事拆成两个属性**。

- `Layout`（`LayoutMode { Left, Right, Stretch, Edge }`，默认 `Edge`）：决定按钮组在父容器里的水平对齐，语义对应 `HorizontalAlignment`。`Edge` 是快捷预设——整组贴到主按钮该在的那一侧。
- `PrimaryPlacement`（`PrimaryPlacementMode { Auto, Leading, Trailing }`，默认 `Auto`）：决定 `Button.IsDefault` 主按钮在组内的相对位置（首/尾）。`Auto` = macOS→Trailing，其余→Leading。找不到主按钮时此项无效。
- `Spacing`（`double`，默认 `8`）：子级间距，命名/默认值对齐 `FlexWrapPanel.ColumnSpacing`。

布局算法：

- `Edge`：主按钮独享平台侧，其余按文档顺序挤到对边；找不到主按钮 → 退化为 `Right`。
- `Left` / `Right`：先按 `PrimaryPlacement` 决定主按钮在序列首/尾，再从对应端连续 packed 排列。
- `Stretch`：等分列，`colWidth = (finalWidth − Spacing·(n−1)) / n`，依赖子级 `HorizontalAlignment=Stretch` 视觉填满。
- `IsCancel` **不做任何特殊处理**——它只影响 Esc 键行为，与布局无关。判据只有 `IsDefault` 一个。

工程细节：`Layout`/`Spacing` 用 `AffectsMeasure<ModalActionPanel>`（改 desired size），`PrimaryPlacement` 用 `AffectsArrange`（只改顺序/定位，不影响测量）。

### `Dialog.axaml` 已接入

动作行从手写 `Grid *,*` 换成：

```xml
<local:ModalActionPanel
    Grid.Row="3"
    Margin="18"
    Layout="Stretch"
    Spacing="8">
    <Button Classes="Primary"
            IsVisible="{TemplateBinding IsPrimaryButtonVisible}"
            Command="{Binding PrimaryCommand, RelativeSource={RelativeSource Mode=TemplatedParent}}"
            IsDefault="True">
        <TextBlock Text="{TemplateBinding PrimaryText}" />
    </Button>
    <Button Command="{Binding SecondaryCommand, RelativeSource={RelativeSource Mode=TemplatedParent}}"
            IsCancel="True">
        <TextBlock Text="{TemplateBinding SecondaryText}" />
    </Button>
</local:ModalActionPanel>
```

注意用的是 `Layout="Stretch"`（非 `Edge`）：Dialog 本就是等分两列视觉，Stretch 保持原观感，靠 `PrimaryPlacement=Auto` 让主按钮在 macOS 下落到右侧列。原先 `InternalConverters.ButtonColumn` / `ButtonColumnSpan` 那套列收缩逻辑已删除（`IsPrimaryButtonVisible=false` 时仅剩一个子级，Stretch 自动单列占满）。

→ **17 个 `<husk:Dialog>` 自动全部修复**，Polymerium 侧零改动。

### 发版

`Huskui.Avalonia` **1.3.0** 已发布。

---

## 现状盘点（Polymerium 侧）

### A 类：`<husk:Dialog>`（`src/Polymerium.Avalonia/Dialogs/`，17 个）

升级 NuGet 后自动修复，无需改 XAML：

> AccountPickerDialog、AssetImporterDialog、FilePickerDialog、GameVersionPickerDialog、LoaderEditorDialog、LoaderPickerDialog、MessageDialog、ModpackExporterDialog、PackageBulkUpdatePreviewerDialog、PackageBulkUpdateReviewerDialog、PackageListExporterDialog、PackagePickerDialog、ProxySettingsDialog、ReferenceVersionPickerDialog、RuntimePickerDialog、TagPickerDialog、UserInputDialog

### B 类：`<husk:Modal>` 自摆按钮（`src/Polymerium.Avalonia/Modals/`，5 个）

| 文件 | 当前布局（左→右） | 容器 |
|------|------------------|------|
| AccountCreationModal | `[Back][Next/Finish·主][Dismiss]` | StackPanel Horizontal |
| OobeModal | `[Back][Next/Finish·主][Skip]` | StackPanel Horizontal |
| AppUpdateModal | `[进度条 *][Primary 确认][取消]` | Grid `*,Auto,Auto` |
| ProfileRulesModal | `[Primary 添加][..*..][计数][Dismiss]` | Grid `Auto,*,Auto,Auto` |
| ProfileRuleSelectorsModal | `[Primary 添加][..*..][计数][Dismiss]` | Grid `Auto,*,Auto,Auto` |

不在范围（仅关闭/Dismiss、无确认主按钮，或主按钮不参与左右语义）：SnapshotsModal、ProfileRuleModal、AboutModal、ProgressModal、TrophyModal、WorkspaceDiffModal（其 Col0/Col1 是 diff 双栏视图，非按钮）。

---

## 阶段二：Polymerium.Avalonia（待执行）

### 1. 升级 NuGet 引用

`src/Polymerium.Avalonia/Polymerium.Avalonia.csproj`：

```xml
<PackageReference Include="Huskui.Avalonia" Version="1.3.0" />
```

（`Huskui.Avalonia.Markdown` / `Huskui.Avalonia.Mvvm` 按需同步。）

### 2. 验证 A 类（应零改动即修复）

升级后逐一打开 `Dialogs/` 下 17 个对话框，确认 macOS 下 Primary 已到右侧、Windows/Linux 维持左侧。

### 3. 接入 B 类的 5 个 Modal

**统一模式**：把「主按钮 + 取消」收进 `<husk:ModalActionPanel Layout="Right">`，放父 Grid 的右侧列；`PrimaryPlacement` 省略（默认 `Auto`，按平台自动）。其余非动作元素（进度条、计数、Back）留在外面。

> 为什么是 `Layout="Right"` 而非 `Edge`：这 5 个 Modal 的按钮组都嵌在父 Grid 的 `Auto` 列里，槽宽 = 内容宽，没有「分居容器两端」的空间；packed 靠右 + `PrimaryPlacement=Auto` 即可让主按钮在 macOS 下到组尾（最右）、Windows 下到组首。`Edge` 留给宽容器场景，本批用不上。

#### AppUpdateModal

`ColumnDefinitions="*,Auto,Auto"` → `*,Auto`，进度条留 Col0，按钮组占 Col1：

```xml
<Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
    <StackPanel Grid.Column="0" ...><!-- 进度条 --></StackPanel>
    <husk:ModalActionPanel Grid.Column="1" Layout="Right" Spacing="8">
        <Button Classes="Primary" ...ConfirmUpdateCommand... IsDefault="True"> ... </Button>
        <Button ...Dismiss... IsCancel="True">取消</Button>
    </husk:ModalActionPanel>
</Grid>
```

- Windows：`[进度条][Primary][取消]`（与现状一致，零回归）
- macOS：`[进度条][取消][Primary]`（修复）

#### ProfileRulesModal / ProfileRuleSelectorsModal

`ColumnDefinitions="Auto,*,Auto,Auto"` → `Auto,*,Auto`，计数 TextBlock 留左，按钮组收进右侧：

```xml
<Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="8">
    <TextBlock Grid.Column="0" ...><!-- 计数 N 条规则 --></TextBlock>
    <husk:ModalActionPanel Grid.Column="2" Layout="Right" Spacing="8">
        <Button Classes="Primary" ...AddRuleCommand...>添加</Button>
        <Button ...Dismiss... IsCancel="True">取消</Button>
    </husk:ModalActionPanel>
</Grid>
```

- Windows：`[计数] ......... [Primary 添加][取消]`（primary 从最左移到右侧组，属合理统一）
- macOS：`[计数] ......... [取消][Primary 添加]`（修复）

#### AccountCreationModal / OobeModal（向导）

**Back 单独拎到左边**，右侧 `ModalActionPanel` 装 `Next/Finish`（主）+ `Dismiss/Skip`。StackPanel 换成 Grid：

```xml
<Grid ColumnDefinitions="Auto,*">
    <!-- Back 单独在左 -->
    <Button Grid.Column="0"
            IsVisible="{Binding ...IsBackAvailable}"
            Command="{Binding ...GoBackCommand}">返回</Button>

    <!-- 右侧组：整体靠右(Layout=Right)，主按钮按平台在组内定位 -->
    <husk:ModalActionPanel Grid.Column="1" Layout="Right" Spacing="8">
        <Button Classes="Primary" IsVisible="{Binding !IsLast}" ...GoNextCommand... IsDefault="True">下一步</Button>
        <Button Classes="Primary" IsVisible="{Binding IsLast}"   ...GoFinishCommand... IsDefault="True">完成</Button>
        <Button ...Dismiss... IsCancel="True">取消</Button>
    </husk:ModalActionPanel>
</Grid>
```

- `Next`/`Finish` 互斥可见，任一时刻只有一个 `IsDefault` 主按钮，`Dismiss` 为「其余」。
- Windows：`[Back] ........ [Next/Finish][Dismiss]`
- macOS：`[Back] ........ [Dismiss][Next/Finish]`（主按钮到最右，修复）

---

## 不在范围

SnapshotsModal、ProfileRuleModal、AboutModal、ProgressModal、TrophyModal、WorkspaceDiffModal。

---

## 验收标准（阶段二）

- 17 个 `<husk:Dialog>` 在 macOS 下 Primary 位于右侧、Windows/Linux 维持左侧（零改动验证）。
- 5 个 Modal 达到上文「Windows/macOS」图示结果。
- 构建通过：`dotnet build "Polymerium.slnx"` 无新增错误（Avalonia Accelerate Community 遥测提示与 Trident 子模块既有警告属既有噪声，不计）。
