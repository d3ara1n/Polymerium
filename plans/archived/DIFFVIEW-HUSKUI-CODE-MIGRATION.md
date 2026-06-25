# DiffView 迁移至 Huskui.Avalonia.Code 计划

> **状态**: 待实施
> **决策**: 方案 A — 将 DiffView 连同 DiffPlex 依赖整体合入 `Huskui.Avalonia.Code` 子包
> **日期**: 2026-06-14

---

## 背景

Polymerium 的 `DiffView` 控件（side-by-side 差异视图，含行号着色、强化状态条、带变更标记的概览滚动条）本质上是"双栏 + 行号 + 状态着色的代码/文本展示控件"。它依赖的 UI 资源（`Control*Brush`、`SmallCornerRadius`）全部来自 Huskui 主包，自身无任何 Polymerium 业务依赖。

经过评估，将其从 Polymerium 抽出、合入 Huskui 的 `Huskui.Avalonia.Code` 子包是最合适的归属。

### 为什么是 `Huskui.Avalonia.Code`

| 判断维度 | 依据 |
|---------|------|
| **定位契合** | `Code` 包的定位是"语法高亮的代码/文本展示控件"（现含 `CodeViewer`）。DiffView 是其同族控件，放在同一包语义自洽 |
| **外部依赖有先例** | `Code` 包已为承载"需要额外依赖的控件"而存在——它引用 `TextMateSharp.Grammars`。DiffPlex 作为"代码相关的外部算法依赖"进这个包完全符合既定模式 |
| **共享 namespace 机制** | `Code` 包控件注册到 `https://github.com/d3ara1n/Huskui.Avalonia`，通过 `[HuskuiExtension]` 让主包 `HuskuiTheme` 自动合并主题资源。DiffView 进来后用户用法和 `CodeViewer` 完全一致（`<husk:DiffView .../>`），无需额外配置 |
| **技术栈重合** | `CodeViewer` 与 `DiffView` 都是 `TemplatedControl` + `ControlTheme` + `PART_` 模式 + 行号 gutter + 等宽字体 `Cascadia Code, Consolas...` |
| **协同潜力** | 合并后 DiffView 的文本区未来可复用 `TextMateInlineFormatter` 做语法高亮（Code 包现成能力），这是放在一起的额外红利 |

### 为什么不走方案 B（解耦 DiffPlex）

方案 B 是让 DiffView 只渲染、把 DiffPlex 调用移到外部 helper。放弃它的理由：DiffPlex 是 DiffView 的核心能力，"自包含 + 装即用"对控件库更有价值；而 `Code` 包的存在本就解决了"外部依赖污染主包"的顾虑，方案 B 的解耦收益在合入 `Code` 后不再成立。

---

## 当前状态盘点

### 涉及的文件（Polymerium 侧，已随 commit `968bfa38` 推送）

| 文件 | 职责 | 当前 namespace |
|------|------|---------------|
| `src/Polymerium.Avalonia/Controls/DiffView.cs` | 主控件：双栏 diff + 滚动 + 概览桥接 | `Polymerium.Avalonia.Controls` |
| `src/Polymerium.Avalonia/Controls/DiffView.axaml` | `ControlTheme`（`ResourceDictionary`） | — |
| `src/Polymerium.Avalonia/Controls/DiffOverviewBar.cs` | 概览滚动条自定义控件 | `Polymerium.Avalonia.Controls` |
| `src/Polymerium.Avalonia/Models/DiffLineKind.cs` | 枚举（Unchanged/Added/Removed/Empty/Modified） | `Polymerium.Avalonia.Models` |
| `src/Polymerium.Avalonia/Models/DiffLineModel.cs` | 单行模型 | `Polymerium.Avalonia.Models` |
| `src/Polymerium.Avalonia/Models/DiffMarker.cs` | 概览标记模型（YRatio/HeightRatio/Kind） | `Polymerium.Avalonia.Models` |
| `src/Polymerium.Avalonia/Converters/InternalConverters.cs` | 含 4 个 diff converter + `EnsureDiffBrushes`（**与业务 converter 混在一起**） | `Polymerium.Avalonia.Converters` |

### 外部依赖

- **DiffPlex 1.9.0**（`Polymerium.Avalonia.csproj:29`）— 仅 `DiffView.cs` 一处使用（`SideBySideDiffBuilder.Instance.BuildDiffModel`）
- **Huskui 资源**（运行期 `TryGetResource` + axaml `StaticResource`）：`ControlBorderBrush`、`SmallCornerRadius`、`ControlSuccess/Danger/Accent{,Foreground,TranslucentFull}BackgroundBrush`、`ControlSecondaryForegroundBrush`、`ControlTranslucent{Full,Half}BackgroundBrush` — **全部由 Huskui 主包提供，Polymerium 自己没有定义**

### 干净度结论

- ✅ 模型类（`DiffLineKind`/`DiffLineModel`/`DiffMarker`）纯 POCO，零外部依赖
- ✅ `DiffOverviewBar` 教科书式自定义控件，零 Polymerium 依赖
- ✅ 资源引用全部来自 Huskui
- ⚠️ `InternalConverters` 是杂烩：4 个 diff converter 与 `AccentColorToBrush`（依赖 Polymerium `AccentColor` 枚举）、`LatencyToColorBrush`（依赖 `ConnectionTestStatus`）等业务 converter 混在同一文件 — **diff 部分需抽出**
- ⚠️ DiffPlex 直接焊在 `DiffView.UpdateDiff()` 里 — 走方案 A 则无需改动，连同依赖一起搬

---

## Huskui 约定差异（合入前必须对齐）

对照 `/Users/chien/Projects/Huskui.Avalonia/AGENTS.md`，DiffView 当前代码有以下不符合规范之处。**阶段 1 的核心工作就是补齐这些**。

| # | 约定要求（AGENTS.md） | 当前现状 | 需调整为 |
|---|---------------------|---------|---------|
| 1 | `PART_` 前缀**仅**用于带 `[TemplatePart]` 属性的契约部件，且必须三件套：① `[TemplatePart(PART_X, typeof(T))]` ② `public const string PART_X = nameof(PART_X);` ③ XAML 用 `Name="{x:Static local:DiffView.PART_X}"` | `DiffView` 用了 `PART_ScrollViewer`/`PART_HScrollBar`/`PART_OverviewBar`，`DiffOverviewBar` 无模板部件 — 但**全部缺少** `[TemplatePart]` 属性和 `const` 声明，XAML 是裸 `Name="PART_..."` | 三个部件补齐三件套 |
| 2 | `OnApplyTemplate` 里事件订阅必须 `-=`/`+=` 成对在同一方法内（防模板重应用时重复订阅） | `DiffView.OnApplyTemplate` 只有 `+=`，无 `-=` | 每个订阅前补 `-=` |
| 3 | Converter 组织成**静态类的静态属性**（如 `CornerRadiusConverters`、`BoolConverters`），用 `RelayConverter`/`RelayMultiConverter`，XAML 用 `{x:Static}` 引用 | 4 个 diff converter + `EnsureDiffBrushes` 混在 `InternalConverters`（已用 `RelayConverter` + `{x:Static}`，但宿主类不纯） | 抽出独立 `DiffConverters` 静态类 |
| 4 | namespace 用 `Huskui.Avalonia.Code.*`，并通过 `AssemblyInfo.cs` 的 `[XmlnsDefinition]` 注册到共享 namespace | `Polymerium.Avalonia.*` | 改 namespace + 注册 |
| 5 | XAML namespace 是 `https://github.com/d3ara1n/Huskui.Avalonia`（前缀 `husk`） | `xmlns:app="https://github.com/d3ara1n/Polymerium"` | 改 xmlns + 全文 `app:` → `husk:`（或 `local:`） |
| 6 | 私有静态字段 `camelCase`（**无下划线**），私有实例字段 `_camelCase` | `DiffView`/`DiffOverviewBar` 的私有静态字段用了 `_camelCase`（如 `_theme`、`DiffAddedBrush` 混用） | 静态字段去下划线 |
| 7 | `StyledProperty` 多行泛型写法（类型参数单独一行） | 已符合 | ✅ 无需改 |
| 8 | 命名常量（`LINE_HEIGHT` 等大写常量）风格 | Huskui 未强制，可保留 | ✅ 可接受 |

### 其他需注意的 Huskui 约定（不阻塞但建议遵守）

- **不要运行格式化工具**（Polymerium AGENTS.md 明确禁止 `Format-Files.ps1`/`csharpier`/`xstyler`，Huskui 仓有 `Settings.XamlStyler` 但只在用户主动调用时跑）
- Color/Size 变体用 CSS classes，Style 变体用不同 `ControlTheme` — DiffView 当前无变体需求，不涉及
- 动画时长引用 `Themes/Basics.axaml` 的命名常量，圆角用 `CornerRadii.axaml` 的 key — DiffView 已用 `SmallCornerRadius`，符合
- 资源 key 命名 `{Owner}{Variant}{State}{Property}Type` — 已对齐

---

## 实施计划

分两阶段，每阶段独立可验证、可回退。

### 阶段 1：在 Polymerium 内规整（零行为变更）

> 目的：先把代码改到符合 Huskui 规范，但仍在 Polymerium 仓内运行。这一步无论是否最终搬移都能提升代码质量，且风险最低。
> 验证标准：`dotnet build` 通过，DiffView 运行行为与 commit `968bfa38` 完全一致。

**步骤：**

1. **`DiffView.cs`** — 补 `[TemplatePart]` 三件套
   ```csharp
   [TemplatePart(PART_ScrollViewer, typeof(ScrollViewer))]
   [TemplatePart(PART_HScrollBar, typeof(ScrollBar))]
   [TemplatePart(PART_OverviewBar, typeof(DiffOverviewBar))]
   public class DiffView : TemplatedControl
   {
       public const string PART_ScrollViewer = nameof(PART_ScrollViewer);
       public const string PART_HScrollBar = nameof(PART_HScrollBar);
       public const string PART_OverviewBar = nameof(PART_OverviewBar);
       // ...
   }
   ```

2. **`DiffView.axaml`** — 裸 `Name="PART_..."` 改 `Name="{x:Static app:DiffView.PART_...}"`

3. **`DiffView.cs` `OnApplyTemplate`** — 每个事件订阅前补 `-=`：
   ```csharp
   if (_hScrollBar != null)
   {
       _hScrollBar.ValueChanged -= OnHScrollBarValueChanged;
       _hScrollBar.ValueChanged += OnHScrollBarValueChanged;
   }
   // 同理处理 _scrollViewer.ScrollChanged 和 _overviewBar.ScrollRequested
   ```
   注意 `ScrollChanged` 和 `ScrollRequested` 是阶段 1 本次新增的订阅，也要一并规整成对。

4. **抽 `DiffConverters`** — 新建 `src/Polymerium.Avalonia/Converters/DiffConverters.cs`，把 `InternalConverters` 里的以下内容整体搬过去：
   - `EnsureDiffBrushes()` 及其全部私有 brush 字段（`DiffAddedBrush`...`DiffSecondaryForegroundBrush`）
   - `DiffLineKindToBackground` / `DiffLineKindToIndicatorBrush` / `DiffLineKindToForeground` 三个 converter
   - 改成 `public static class DiffConverters`（静态类，非现在的隐式静态类）
   - 静态字段命名按 Huskui 约定去下划线（`_theme` → `theme` 等）

5. **`InternalConverters.cs`** — 删除已搬走的 diff 相关成员，保留业务 converter。**注意**：`EnsureDiffBrushes` 是私有的，确认没有别的成员依赖它（grep 验证）。

6. **`DiffView.axaml`** — converter 引用从 `{x:Static app:InternalConverters.DiffLineKindTo*}` 改为 `{x:Static app:DiffConverters.DiffLineKindTo*}`

7. **私有静态字段重命名** — `DiffView.cs`/`DiffOverviewBar.cs` 里的 `_theme`、`_maxContentWidth` 等若是 `static`，按 Huskui 约定去下划线（实例字段保留 `_`）

8. **build 验证 + 运行验证** — 确认 DiffView 行为不变

### 阶段 2：物理搬入 Huskui.Avalonia.Code

> 前置：Huskui 仓（`/Users/chien/Projects/Huskui.Avalonia`）准备好接收，确认分支策略。
> 验证标准：Huskui 仓 build 通过 + Gallery 示例可展示 + Polymerium 改用 NuGet 包后行为一致。

**步骤：**

1. **Huskui 仓 `Directory.Packages.props`** — 加 DiffPlex 版本（参考现有 TextMateSharp 的条目格式）

2. **`src/Huskui.Avalonia.Code/Huskui.Avalonia.Code.csproj`** — 加 `<PackageReference Include="DiffPlex" />`（版本走中央管理）

3. **搬文件到 Huskui**（建议放在与 `CodeViewer` 对称的位置）：
   | 源（Polymerium） | 目标（Huskui.Avalonia.Code） |
   |----------------|----------------------------|
   | `Controls/DiffView.cs` | `Controls/DiffView.cs` |
   | `Controls/DiffView.axaml` | `Controls/DiffView.axaml` |
   | `Controls/DiffOverviewBar.cs` | `Controls/DiffOverviewBar.cs` |
   | `Models/DiffLineKind.cs` | `Models/DiffLineKind.cs`（或嵌套进 DiffView，见下） |
   | `Models/DiffLineModel.cs` | `Models/DiffLineModel.cs` |
   | `Models/DiffMarker.cs` | `Models/DiffMarker.cs` |
   | `Converters/DiffConverters.cs` | `Converters/DiffConverters.cs` |

   > 嵌套取舍：Polymerium AGENTS.md 要求"一类一文件，按语义归属决定是否嵌套"。`DiffLineKind`/`DiffLineModel`/`DiffMarker` 是否嵌套进 `DiffView`/`DiffOverviewBar`，取决于它们是否被外部消费者使用。由于 `DiffLineModel` 是公开 API（用户可能自己喂 `Lines`），建议独立文件；`DiffMarker` 是概览内部概念，可考虑嵌套进 `DiffOverviewBar`。

4. **改 namespace** — 所有搬过去的文件：
   - `Polymerium.Avalonia.Controls` → `Huskui.Avalonia.Code.Controls`
   - `Polymerium.Avalonia.Models` → `Huskui.Avalonia.Code.Models`（或嵌套就不需要）
   - `Polymerium.Avalonia.Converters` → `Huskui.Avalonia.Code.Converters`

5. **`src/Huskui.Avalonia.Code/Properties/AssemblyInfo.cs`** — 加 `[XmlnsDefinition]`（若新 namespace 尚未注册到共享 namespace）。参考 CodeViewer 的注册方式。

6. **`Themes/Bundle.axaml`**（Huskui.Avalonia.Code 的主题打包文件）— 把 DiffView 的 `ControlTheme` 合并进去（或保持 DiffView.axaml 作为独立 ResourceDictionary 被 Bundle 合并，看 Code 包现有 `CodeViewer.axaml` 的处理方式）

7. **DiffView.axaml 内部**：
   - `xmlns:app="https://github.com/d3ara1n/Polymerium"` → 改为 Huskui 共享 namespace（`xmlns:husk="https://github.com/d3ara1n/Huskui.Avalonia"`，或用 `local:` 指向自身程序集）
   - 全文 `app:` 前缀替换
   - `x:DataType="app:DiffLineModel"` 等同步改

8. **Gallery 示例**（`src/Huskui.Gallery`）— 加一个 DiffView 的 demo 页，参考 CodeViewer 的示例页写法。这是 Huskui 的惯例，每个控件有 Gallery 演示。

9. **Huskui 仓 build + Gallery 运行验证**

10. **Polymerium 侧切换消费**：
    - 删除阶段 1 搬走的 7 个文件
    - `InternalConverters.cs` 确认无 diff 残留
    - `Polymerium.Avalonia.csproj` 删除 `<PackageReference Include="DiffPlex">`（改由 Huskui.Avalonia.Code 传递依赖）
    - 加 `<PackageReference Include="Huskui.Avalonia.Code" Version="..." />`
    - `WorkspaceDiffModal.axaml` 的 `controls:DiffView` 改 `husk:DiffView`（xmlns 已有 husk 前缀）
    - 全局 grep 确认无残留 `Polymerium.Avalonia.Controls.Diff` / `Polymerium.Avalonia.Models.Diff` 引用
    - build + 运行验证 DiffView 行为一致

---

## 已知坑（实施时注意）

### Avalonia 12 移除了 `PointerMovedEventArgs`

`DiffOverviewBar.OnPointerMoved` 的参数类型在 Avalonia 11 是 `PointerMovedEventArgs`，**Avalonia 12 已移除该类型**，改用基类 `PointerEventArgs`。

- Huskui 仓的 `Directory.Packages.props` 用的 Avalonia 版本需确认（Polymerium 用 12.0.4）。若 Huskui 仍兼容 11，搬入时此处的 `PointerEventArgs` 可能需要条件处理；若 Huskui 已是 12，直接搬即可。
- 证据：`src/Avalonia.Base/Input/InputElement.cs` 中 `PointerMovedEvent` 注册为 `RoutedEvent<PointerEventArgs>`。

### Huskui 目标框架是 `net8.0;net10.0` 双框架

`Huskui.Avalonia.Code.csproj` 用 `$(HuskuiTargetFrameworks)`（来自 `Directory.Build.props`），是双目标。搬入的 DiffView 代码需确保在两个框架下都能编译（`DiffPlex` 也需支持双框架 — 1.9.0 支持）。

### mtime-based Diff 判定（Polymerium 业务侧，非控件问题）

Polymerium 的 Workspace 用文件 mtime 判定改动（`InstanceWorkspacePageModel.Diff()`），不是内容哈希。**这与 DiffView 控件无关**，只是测试时要注意：要让 live 文件的 mtime 晚于 import 文件，否则不会列进变更。测试数据生成脚本里用 `touch -t` 强制设置 mtime。

---

## 验证清单

### 阶段 1 完成后
- [ ] `dotnet build "src/Polymerium.Avalonia/Polymerium.Avalonia.csproj"` 0 error 0 warning
- [ ] `[TemplatePart]` 三件套齐全（grep `TemplatePart` + `public const string PART_`）
- [ ] `OnApplyTemplate` 内所有事件订阅都是 `-=`/`+=` 成对
- [ ] `DiffConverters` 独立成静态类，`InternalConverters` 无 diff 残留
- [ ] 运行 Polymerium，打开 Workspace 的 DiffView，行号着色/状态条/概览滚动条行为与 `968bfa38` 一致

### 阶段 2 完成后（Huskui 侧）
- [ ] Huskui 仓 `dotnet build Huskui.slnx` 通过
- [ ] Gallery 新增 DiffView demo 页能正常渲染
- [ ] `DiffPlex` 进了 `Huskui.Avalonia.Code` 的依赖，主包 `Huskui.Avalonia` 不受污染
- [ ] namespace 全部是 `Huskui.Avalonia.Code.*`
- [ ] `[XmlnsDefinition]` 注册到共享 namespace，`<husk:DiffView>` 可识别

### 阶段 2 完成后（Polymerium 侧）
- [ ] `Polymerium.Avalonia.csproj` 不再直接引用 `DiffPlex`
- [ ] 7 个 diff 相关文件已从 Polymerium 仓删除
- [ ] `InternalConverters.cs` 不再含 diff converter
- [ ] `WorkspaceDiffModal.axaml` 用 `husk:DiffView`
- [ ] 全局 grep 无 `Polymerium.Avalonia.(Controls|Models|Converters).Diff*` 残留引用
- [ ] build + 运行，DiffView 行为与迁移前一致

---

## 关联

- 代码基线 commit: `968bfa38`（feat: 优化差异视图的行号样式并新增概览滚动条）
- Huskui 仓: `/Users/chien/Projects/Huskui.Avalonia`
- 目标子包: `src/Huskui.Avalonia.Code/`
- Huskui 约定权威: `/Users/chien/Projects/Huskui.Avalonia/AGENTS.md`
- 参考控件（同包）: `src/Huskui.Avalonia.Code/Controls/CodeViewer.cs`
