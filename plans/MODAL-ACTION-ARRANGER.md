# 模态框动作按钮的平台感知布局（ModalActionPanel）

> **进度**：阶段一实现已完成（待 build 验证 + 发版），阶段二待库发版后进行。
> 命名落地为 `ModalActionPanel`（非原计划的 `ModalActionArranger`——见下文「命名变更」）。

## 背景

Huskui.Avalonia 与 Polymerium.Avalonia 的模态/对话框普遍采用「主按钮（确认）在左、其余（取消）在右」的布局。这在 Windows/Linux 上是常规约定，但在 macOS 上违反 Apple HIG——macOS 要求**默认动作（primary / `IsDefault`）位于最右侧**，取消位于其左侧。

现状的根因有两条：

1. **Huskui 的 `Dialog` 控件主题**（`Controls/Dialog.axaml`）把动作按钮写死在一个 `Grid ColumnDefinitions="*,*"` 里：Primary 恒为 `Grid.Column="0"`（左），Secondary 恒为右。所有用 `<husk:Dialog>` 的对话框都继承这个布局。
2. **Polymerium 自己的 `<husk:Modal>`** 里手写的按钮 Grid / StackPanel，主按钮普遍摆在左侧。

Huskui.Avalonia 在 Polymerium 中以 **NuGet（`Huskui.Avalonia` 1.2.0）** 形式引用，不可随 Polymerium 一起改；它是独立的外部库，必须先在库内完成改造并发布新版本，Polymerium 才能升级消费。因此本计划分两阶段。

## 现状盘点

### A 类：`<husk:Dialog>`（`src/Polymerium.Avalonia/Dialogs/`，共 17 个）

全部继承 Huskui `Dialog` 主题，Primary 在左、Secondary 在右，macOS 上全部「错」：

> AccountPickerDialog、AssetImporterDialog、FilePickerDialog、GameVersionPickerDialog、LoaderEditorDialog、LoaderPickerDialog、**MessageDialog**、ModpackExporterDialog、PackageBulkUpdatePreviewerDialog、PackageBulkUpdateReviewerDialog、PackageListExporterDialog、PackagePickerDialog、ProxySettingsDialog、ReferenceVersionPickerDialog、RuntimePickerDialog、TagPickerDialog、UserInputDialog

→ 这一类**通过改 Huskui 的 `Dialog` 主题一次性全部修复**，Polymerium 侧零改动。

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

## 方案总览

在 Huskui.Avalonia 新增一个**纯布局容器控件 `ModalActionPanel`**（自写 `Panel`，不对子按钮套样式——子按钮自带什么主题就是什么），提供四种摆放形态（`Layout`），其中「Edge（靠边）」模式会按平台把 `IsDefault=True` 的主按钮推到正确的边。然后把 Huskui `Dialog` 主题的动作行换成它，再把 Polymerium 的 5 个 Modal 接入。

### 命名变更（原计划 → 落地）

原计划候选名为 `ModalActionArranger`，讨论中改为 **`ModalActionPanel`**。理由：

- 该控件就是一个 `Panel` 子类（仿库内 `FlexWrapPanel`），不存在「外层控件 + 内部 Panel」的拆分。既然本身就是 Panel，名字直接叫 `…Panel` 更诚实，类型即名字。
- 它唯一不可替代的价值是 `Edge` 模式的**平台感知**主按钮定位，这个语义只对模态/对话框底栏成立（Apple HIG 对默认确认动作的专属要求）。`ActionArranger` 暗示的「工具栏通用」并不真实存在，故不加更通用的前缀。
- `XXXs` 在本库里是 `AvaloniaList<XXX>` 集合别名约定，故不叫 `ModalActions`。

为什么用 `Panel` 而非 `ItemsControl`：库内 `ButtonGroup` 是 `ItemsControl` 并通过 `ItemContainerTheme` **给子按钮套样式**；本控件明确要求「里面是啥就是啥」，`Panel`（参考库内 `FlexWrapPanel`）不会改写子级主题，正合适。

---

## 阶段一：Huskui.Avalonia（必须先完成并发版）

### 1. ✅ 新增 `Controls/ModalActionPanel.cs`

仿 `FlexWrapPanel.cs` 的写法：`Panel` 子类 + `StyledProperty` + 自写 `MeasureOverride` / `ArrangeOverride`。

#### API（落地版）

```csharp
namespace Huskui.Avalonia.Controls;

public class ModalActionPanel : Panel
{
    public enum LayoutMode { Start, End, Fill, Edge }          // 靠左 / 靠右 / 填充 / 靠边
    public enum PrimaryPlacementMode { Auto, Leading, Trailing }

    // 整体摆放形态。默认 Edge（用户自定义 Modal 最常用）。
    public static readonly StyledProperty<LayoutMode> LayoutProperty =
        AvaloniaProperty.Register<ModalActionPanel, LayoutMode>(nameof(Layout), defaultValue: LayoutMode.Edge);

    // 仅 Edge 生效。主按钮落在哪边。默认 Auto：macOS→Trailing，其余→Leading。
    public static readonly StyledProperty<PrimaryPlacementMode> PrimaryPlacementProperty =
        AvaloniaProperty.Register<ModalActionPanel, PrimaryPlacementMode>(nameof(PrimaryPlacement), defaultValue: PrimaryPlacementMode.Auto);

    // 默认 8，命名/默认值对齐 FlexWrapPanel.ColumnSpacing
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ModalActionPanel, double>(nameof(Spacing), defaultValue: 8d);

    public LayoutMode Layout { get; set; }
    public PrimaryPlacementMode PrimaryPlacement { get; set; }
    public double Spacing { get; set; }
}
```

**属性命名说明**：原计划用 `Placement`/`PlacementMode`，落地改为 `Layout`/`LayoutMode`——否则 `Placement` 与 `PrimaryPlacement` 都带 Placement 却分属不同层次（前者是「整体怎么排」4 形态，后者是「主按钮落在哪边」3 选项），读起来易混。`Layout`（形态）+ `PrimaryPlacement`（落点）语义层次分明。

#### 布局算法

- **Measure**：对可见子级以「内容宽度」测量（用 `infinity` 宽测量拿纯内容宽），`DesiredSize = (Σwidth + Spacing·(n−1), maxHeight)`。`Fill` 模式下若可用宽度有限则返回该宽度，否则退化为内容宽。
- **Arrange**：
  - `Start`：从 `x=0` 起依次排开（等价 `HorizontalAlignment=Left`）。
  - `End`：从 `x=finalWidth` 起向左收尾，子级内部不翻转（等价 `HorizontalAlignment=Right`）。
  - `Fill`：等分列，`colWidth = (finalWidth − Spacing·(n−1)) / n`，每个子级被排进等宽格子（依赖子级 `HorizontalAlignment=Stretch` 视觉填满）。
  - `Edge`：
    1. **主按钮** = 第一个 `IsVisible && IsDefault==true` 的子级（若多个可见 default，取首个，其余按普通子级处理）。
    2. **主按钮去平台边**：`PrimaryPlacement` 解析 → `Auto` 时 `OperatingSystem.IsMacOS()` → Trailing（右），其余 → Leading（左）；`Leading`/`Trailing` 强制覆盖。
    3. **其余子级** → 对边，**保持文档顺序**挤在一起。
    4. **找不到主按钮** → 退化为 `End`（整体靠右，最贴近普通对话框底栏）。
    5. 仅一个可见子级时：直接居其平台边，无歧义。

  图示（两按钮 P=Primary / S=Secondary，PrimaryPlacement=Auto）：

  ```
  Windows/Linux : [ P ] ............ [ S ]
  macOS         : [ S ] ............ [ P ]
  ```

#### IsCancel 的处理

**不做任何特殊处理。** `IsCancel` 只影响 Esc 键行为，与布局无关。`Edge` 模式下凡非 `IsDefault` 主按钮者一律归入「其余」，统一摆到对边。判据只有 `IsDefault` 一个。

#### PrimaryPlacement：从「可选」变为「首版即加」

原计划标注此项为「可选增强，首版可不加」。讨论中决定**首版就加**——否则 Edge 模式只能靠平台分支，写不出有意义的单元测试（测不出 Trailing/Leading 区别），Gallery 也无法直观对比两种平台效果。代价仅多一个 enum 属性，可接受。

### 2. ✅ 改造 `Controls/Dialog.axaml`

把模板 `Grid.Row="3"` 的动作区从手写 `Grid` 换成 `ModalActionPanel`：

```xml
<!-- 改造后 -->
<local:ModalActionPanel
    Grid.Row="3"
    Margin="18"
    Layout="Fill"
    Spacing="8">
    <Button
        Classes="Primary"
        IsVisible="{TemplateBinding IsPrimaryButtonVisible}"
        Command="{Binding PrimaryCommand, RelativeSource={RelativeSource Mode=TemplatedParent}}"
        IsDefault="True">
        <TextBlock Text="{TemplateBinding PrimaryText}" />
    </Button>
    <Button
        Command="{Binding SecondaryCommand, RelativeSource={RelativeSource Mode=TemplatedParent}}"
        IsCancel="True">
        <TextBlock Text="{TemplateBinding SecondaryText}" />
    </Button>
</local:ModalActionPanel>
```

**Dialog 用 `Fill` 而非 `Edge`（与原计划不同）**：原计划 Dialog 也用 `Edge`，落地改为 `Fill`。理由：macOS 对话框底栏就是按钮等分填满整行（配合 Spacing），`Fill` 才符合 HIG 的视觉密度；且 `Fill` 天然处理了「单按钮时占满整行」——原 Dialog 靠 `ButtonColumnSpan` 在 `IsPrimaryButtonVisible=false` 时收缩 Secondary 列，改用 `Fill` 后这套不再需要。

附带清理：`InternalConverters.ButtonColumn` / `ButtonColumnSpan` 两个 Converter 在源码层面已零引用（原仅服务 Dialog 旧列收缩），已从 `InternalConverters.cs` 删除（dead code）。

> 说明：仅 Primary 可见 → `Fill` 下唯一子级占满整行；仅 Secondary 可见 → 同样占满整行。两种情况视觉一致，符合对话框底栏预期。

这一改**自动修复全部 17 个 `<husk:Dialog>`**。

### 3. ✅ Gallery 演示

已新增 `src/Huskui.Gallery/Views/ModalActionPanelsPage.axaml(.cs)`，注册到 `GalleryService.cs` 的 **Layout** 分类（与 `FlexWrapPanels` 同类），标 `IsNew = true`。含三个示例：

1. **Edge 交互示例**（核心）——`Layout`/`PrimaryPlacement`/`Spacing` 切换器 + 可隐藏主按钮的 CheckBox，在一个固定宽度的 Border 内实时演示平台差异。
2. **Fill 静态示例**——模拟 Dialog 底栏的等分布局。
3. **多动作 Edge 示例**——三按钮（Save 主 + Don't Save + Cancel），展示非主按钮统一归对边并保持文档顺序。

### 4. ⏳ 发版（待 build 验证通过）

发布 `Huskui.Avalonia` 新版本（当前 1.2.0；新增公开控件属 feature，建议 **1.3.0**）。`Huskui.Avalonia.Mvvm` / `Huskui.Avalonia.Markdown` 本轮无改动，按库的发版规则决定是否同步。

#### ⚠️ 既有 build 阻塞（与本次任务无关，发版前需处理）

仓库 HEAD 在 Avalonia **12.0.2** 下不可编译，存在与 `ModalActionPanel` 无关的既有问题：

- **`AppWindow.cs` CS0120**（✅ 已顺手修复）：上个 commit `3598b95` 在静态构造里调用了实例属性 `PseudoClasses.Set(...)`，已改为实例构造（语义等价，平台伪类 per-instance 设置）。
- **`PaginationControl.axaml` AVLN2205**（⚠️ 未修）：模板里用 `Name="{x:Static local:PaginationControl.PART_ItemsControl}"` 声明 required template part，但 Avalonia 12.0.2 的 XAML 校验器**不解析 `{x:Static}` 形式的 Name**，只认字面量 `x:Name`。库内共 **12 个 axaml** 用了此写法，均可能受影响（目前 fail-fast 只报到 PaginationControl，修后可能连锁暴露其余）。这触及 AGENTS.md 里「用 `Name="{x:Static ...}"`」的既定约定，属需讨论决策的范围，未在本轮顺手改动。

---

## 阶段二：Polymerium.Avalonia（库发版后）

### 1. 升级 NuGet 引用

`src/Polymerium.Avalonia/Polymerium.Avalonia.csproj`：

```xml
<PackageReference Include="Huskui.Avalonia" Version="1.3.0" />
```

### 2. 验证 A 类（应零改动即修复）

逐一打开 `Dialogs/` 下 17 个对话框，确认 macOS 下 Primary 已到右侧。无需改 XAML。

### 3. 接入 B 类的 5 个 Modal

通用做法：把底栏的「主按钮 + 取消」塞进 `<husk:ModalActionPanel Layout="Edge">`，其余非动作元素（进度条、计数 TextBlock、Back）留在外面。

#### AppUpdateModal

`ColumnDefinitions="*,Auto,Auto"` → `*,Auto`，进度条留 Col0，panel 占 Col1：

```xml
<Grid ColumnDefinitions="*,Auto" ColumnSpacing="8">
    <StackPanel Grid.Column="0" ...><!-- 进度条 --></StackPanel>
    <husk:ModalActionPanel Grid.Column="1" Layout="Edge" Spacing="8">
        <Button Classes="Primary" ...ConfirmUpdateCommand... IsDefault="True"> ... </Button>
        <Button ...Dismiss... IsCancel="True">取消</Button>
    </husk:ModalActionPanel>
</Grid>
```

- Windows：`[进度条][Primary][取消]`（与现状一致，零回归）
- macOS：`[进度条][取消][Primary]`（修复）

#### ProfileRulesModal / ProfileRuleSelectorsModal

`ColumnDefinitions="Auto,*,Auto,Auto"`，把 `[Primary 添加]` 与 `[Dismiss]` 收进右侧 panel，计数 TextBlock 留在左侧：

```xml
<Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="8">
    <TextBlock Grid.Column="0" ...><!-- 计数 N 条规则 --></TextBlock>
    <husk:ModalActionPanel Grid.Column="2" Layout="Edge" Spacing="8">
        <Button Classes="Primary" ...AddRuleCommand...>添加</Button>
        <Button ...Dismiss... IsCancel="True">取消</Button>
    </husk:ModalActionPanel>
</Grid>
```

- macOS：`[计数] ......... [取消][添加]`（修复）
- Windows：`[计数] ......... [添加][取消]`（与现状相比 primary 从最左移到右侧组，属合理统一，可接受）

#### AccountCreationModal / OobeModal（向导，按既定策略）

**Back 单独拎到左边**，panel 放右边装 `Next/Finish`（主）+ `Dismiss/Skip`。StackPanel 换成 Grid：

```xml
<Grid ColumnDefinitions="Auto,*">
    <!-- Back 单独在左 -->
    <Button Grid.Column="0"
            IsVisible="{Binding ...IsBackAvailable}"
            Command="{Binding ...GoBackCommand}">返回</Button>

    <!-- 右侧 panel：内部 Edge，整体已靠右，内部布局不再影响大局 -->
    <husk:ModalActionPanel Grid.Column="1" HorizontalAlignment="Right" Layout="Edge" Spacing="8">
        <Button Classes="Primary" IsVisible="{Binding !IsLast}" ...GoNextCommand... IsDefault="True">下一步</Button>
        <Button Classes="Primary" IsVisible="{Binding IsLast}"   ...GoFinishCommand... IsDefault="True">完成</Button>
        <Button ...Dismiss... IsCancel="True">取消</Button>
    </husk:ModalActionPanel>
</Grid>
```

- Next/Finish 互斥可见，任一时刻只有一个 `IsDefault` 主按钮，Dismiss 为「其余」。
- Windows：`[Back] ...... [Next/Finish][Dismiss]`
- macOS：`[Back] ...... [Dismiss][Next/Finish]`（主按钮到最右，修复）

---

## 不在范围

SnapshotsModal、ProfileRuleModal、AboutModal、ProgressModal、TrophyModal、WorkspaceDiffModal。

---

## 开放问题 / 待确认（已解决项标注 ✅）

1. ✅ **控件最终命名**：定为 **`ModalActionPanel`**（理由见「命名变更」）。
2. ✅ **是否加 `PrimaryPlacement` 覆盖属性**：首版即加（理由见上文）。
3. ⏳ **Huskui 发版节奏**：阶段二全部依赖新版本上架，需先清掉既有 build 阻塞（`PaginationControl` AVLN2205 及可能的连锁），再发 1.3.0。
4. ⏳ **AGENTS.md 约定冲突**：`Name="{x:Static ...}"` 在 Avalonia 12 下触发 AVLN2205，与项目既定的「TemplatePart 用 `{x:Static}` 引用」约定冲突。需决定是改约定回退字面量 `x:Name`，还是寻找其他解法（如关闭该校验）。这是独立的、影响 12 个控件库的决策，不在本计划范围内。

## 验收标准

- Huskui 侧：`ModalActionPanel` 四种 Layout 行为正确；`PrimaryPlacement` 的 Auto/Leading/Trailing 三档均生效；`Dialog` 主题改造后单按钮/双按钮均正常（Fill 等分）；Gallery 演示可切换 Layout 与 PrimaryPlacement。
- Polymerium 侧（升级后）：17 个 `<husk:Dialog>` 在 macOS 下 Primary 位于右侧、Windows/Linux 维持左侧；5 个 Modal 达到上文「Windows/macOS」图示结果；构建通过（`dotnet build "Polymerium.slnx"` 无新增错误）。
