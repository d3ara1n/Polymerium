# JarInJar Scanner

> 制定日期：2026-07-05
> 定位：补齐 DeveloperToolboxWidget 里的 JarInJar Scanner，扫描实例 mods 目录下 jar 内嵌的 mod，追溯到宿主 mod。
> 当前状态：蓝本（已实施 2026-07-05）

参见宏观位置：[ADVANCED-CAPABILITY-ROADMAP.md](ADVANCED-CAPABILITY-ROADMAP.md) 的「A3 — DeveloperToolbox 补齐 / 第一步」。该文档原有「类冲突诊断」的价值描述已被本计划修正为「隐藏 mod 追溯」（见 §1.2）。

---

## 1. 背景与动机

### 1.1 现状：空壳

`Widgets/DeveloperToolboxWidget.axaml.cs` 的 `OpenJarInJarScanner` 会弹出 `Modals/JarInJarScannerWidgetModal`，但：

- 调用方式是 `new JarInJarScannerWidgetModal { Key = Context.Key }` + `OverlayService.PopModal(modal)` —— **绕过了 activator**，没有配 ModalModel，和项目其它 modal 的范式（`PopModal<TModal>(parameter)` + 命名约定配对 Model）不一致。
- modal 代码里 `ScanAsync` 只 `Directory.GetFiles(*.jar)`，拿到文件列表后直接丢弃，**零解析逻辑**。
- modal 的 axaml 只有标题 + 一个 indefinite 转圈圈占位，没有结果展示区。

### 1.2 真实场景与价值（修正了 ROADMAP 的描述）

ROADMAP 原文把价值写成「排查明明没装 X 却报 X 的**类冲突**」，这个表述容易被读成「库层面的多版本类冲突」。实际讨论后确认的真实场景是：

> 整合包作者打开游戏内 mod 菜单，看到 28 个 mod，但 mods 目录只有 25 个 jar 文件。多出来的 3 个是 jar-in-jar 隐藏 mod，其中某个导致了报错。**作者要知道这多出来的 mod 是哪个外层 mod 带进来的，才能移除那个外层 mod。**

核心价值链：**暴露 jar-in-jar 隐藏的 mod → 追溯到宿主 mod → 告诉作者移除谁**。blame 的目标是 mod，不是库——库自包含在 mod 内，库出问题作者除了移除宿主 mod 无能为力，所以库不是诊断目标。

### 1.3 关键复用点

项目已有的基础设施，本计划直接复用：

| 复用 | 位置 | 说明 |
|---|---|---|
| **mod 元数据解析** | `Utilities/AssetModHelper.cs` | `ParseMetadata` 已支持 Fabric / Quilt / Forge / NeoForge / 旧版 Forge；本计划加 `ZipArchive` 重载，使外层 jar 与内嵌 jar 走**同一个 parse 方法** |

---

## 2. 目标 / 非目标

### 目标

1. 扫描实例 `mods/` 下所有 jar，识别其中**通过 jar-in-jar 嵌套进来的 mod**（不是库）。
2. 每个隐藏 mod 标注**宿主**（哪个外层 mod 内嵌了它），让作者知道移除谁。
3. 搜索条：实时过滤 modId / Name。
4. 重复检测：内嵌 modId 互相重复 / 内嵌 modId 与 mods 目录顶层 modId 重复 → 视觉区分高亮。
5. 走标准 activator 流程，配 ModalModel，与其它 modal 一致。
6. Release 也显示（解除 `DeveloperToolboxWidget` 的 DEBUG-only）。

### 非目标（不做）

- **不显示库**：parse 不出 modId 的条目一律丢弃，不折叠、不留 Tab、不进阶展示。
- **不做库层面的多版本冲突诊断**：库不是 blame 目标。
- **不递归扫描**：jar-in-jar-in-jar 几乎不存在，内嵌 jar 只 parse 一层，不再下钻。
- **不修改/移除 mod**：纯只读诊断，不做任何写操作。
- **不做 By Container / By Library 的双视图**：单一「隐藏 mod 清单」视图足以服务核心场景。

---

## 3. 核心设计

### 3.1 统一 parse：复用并扩展 AssetModHelper

`AssetModHelper` 拆出 `ParseMetadata(ZipArchive)` 核心重载，旧 `ParseMetadata(string)` 变薄封装（`ZipFile.OpenRead` + 调核心）。`ParseMetadata(ZipArchive)` 按顺序探测元数据入口，命中任一即返回：

1. `fabric.mod.json` → Fabric
2. `quilt.mod.json` → Quilt
3. `META-INF/mods.toml` / `mods.toml` → Forge
4. `META-INF/neoforge.mods.toml` / `neoforge.mods.toml` → NeoForge
5. `mcmod.info` → 旧版 Forge

全不命中 → 返回空模型（`ModId == null`）→ 调用方判定「不是 mod」，丢弃。

### 3.2 新增 EnumerateEmbeddedJars：定位内嵌 jar

```csharp
public static IReadOnlyList<ZipArchiveEntry> EnumerateEmbeddedJars(ZipArchive archive)
```

识别三种嵌套声明：

| 声明来源 | 字段路径 | 元素结构 |
|---|---|---|
| Forge / NeoForge JarJar | `META-INF/jarjar/metadata.json` | `{ "jars": [{ "path": "META-INF/jarjar/xxx.jar", ... }] }` |
| Fabric | `fabric.mod.json` | `{ "jars": [{ "file": "nested/xxx.jar" }] }` |
| Quilt | `quilt.mod.json` | `{ "quilt_loader": { "jars": [{ "file": "nested/xxx.jar" }] } }` |

拿到 path/file 后用 `archive.GetEntry(path)` 解析为 entry。三种声明可同时存在（极少见），合并去重。

> NOTE: 路径查找是**大小写敏感**的，这与加载器行为一致——经查证 Fabric Loader（`ModDiscoverer.java` 用 `ZipFile.getEntry`）和 Forge/NeoForge JarJar（`JarSelector` 链路 `getResource`）对 nested jar 路径都是大小写敏感的精确匹配，作者写错大小写时加载器自己也加载不了。所以扫描器大小写敏感**不是漏报**，是与加载器一致的正确行为，不做归一化兜底。

### 3.3 扫描流程

**顺序解析**（不开并行），保证 `i/N` 进度严格递增、当前文件名有意义、内存占用低（不同时打开多个 zip）。每个 jar 独立 `try/catch`，单个 jar 损坏不中断整次扫描。

```
modsDir = Path.Combine(PathDef.Default.DirectoryOfBuild(Key), "mods")
files = Directory.Exists(modsDir)
    ? Directory.GetFiles(modsDir, "*.jar", SearchOption.TopDirectoryOnly)
    : []
topLevelModIds = {}                       // 用于「内嵌 vs 顶层」重复检测
results = []

foreach (file, i) in files:
    report progress: current=file.Name, scanned=i, total
    try:
        using outerArchive = ZipFile.OpenRead(file.FullName)
        outerMod = AssetModHelper.ParseMetadata(outerArchive)
        if outerMod.ModId != null: topLevelModIds.Add(outerMod.ModId)

        foreach entry in AssetModHelper.EnumerateEmbeddedJars(outerArchive):
            try:
                using innerArchive = new ZipArchive(entry.Open(), Read)
                innerMod = AssetModHelper.ParseMetadata(innerArchive)   // 同一个方法
                if innerMod.ModId != null:
                    results.add(HiddenModEntry {
                        ModId, Name, Version, Loader,
                        Host = outerMod.ModId ?? file.Name,
                    })
            catch: skip（坏的内嵌 jar）
    catch: skip（坏的外层 jar）

compute duplicates（§3.4）
_source.Edit(inner => { inner.Clear(); inner.AddRange(sorted); })   // 原子替换
switch to Result/Empty state
```

> NOTE: 扫描必须跟随符号链接，**不能用** `AssetHelper.ScanNonSymlinkFiles`——它显式跳过符号链接，而部署模型下 mods 目录的 jar 恰好是指向 `build/mods/xxx.jar` 的符号链接，跳过会漏扫整批 mod。`Directory.GetFiles` + `ZipFile.OpenRead` 透明跟随 reparse point 打开真实文件。

> 「外层 jar 也 parse」有两个用途：① 给内嵌 mod 的 `Host` 字段提供友好标识（modId 优先于文件名）；② 收集 `topLevelModIds` 用于重复检测。

### 3.4 重复检测 + 视觉区分

扫描完成后对 `results` 做两轮聚合：

1. **内嵌之间**：按 `ModId` 分组，同 modId 出现 ≥2 次 → `DuplicateKind.Inner`。
2. **内嵌 vs 顶层**：`result.ModId ∈ topLevelModIds` → `DuplicateKind.WithTopLevel`。

两种重复在 UI 上**用颜色视觉区分**（非 tooltip——软件设计原则是优先视觉差异，迫不得已才用 tooltip）：

| 重复类型 | 含义 | 严重度 | 卡片配色 |
|---|---|---|---|
| `Inner` | 多个 mod 内嵌了同一个 mod（jar-in-jar 间冲突，加载器 JarJar selector 会仲裁选一个） | 中 | Warning 橙（`ControlWarningTranslucentFullBackgroundBrush` + `ControlWarningBorderBrush`） |
| `WithTopLevel` | 内嵌的 mod 与 mods 目录顶层已装的 mod 重复（真重复加载冲突，手装了又被内嵌） | 高 | Danger 红（`ControlDangerTranslucentFullBackgroundBrush` + `ControlDangerBorderBrush`） |

顶部汇总的 `⚠ N duplicates` = `!= None` 的条目数。

### 3.5 状态机（四态，SwitchContainer 切换）

| 状态 | 触发 | UI |
|---|---|---|
| **Idle** | 进入 modal | 图标 + 一句话说明 + `[Scan]` 按钮 |
| **Scanning** | 点 Scan / Rescan | indeterminate spinner + `i/N` 进度条 + 当前文件名 + `[Cancel]` |
| **Empty** | 扫描完成但结果为空 | 空状态文案（未部署/目录空/无内嵌）+ `[Rescan]` |
| **Result** | 结果非空 | 顶部汇总徽标 + 搜索条 + 卡片清单 + `[Rescan]` |

四态用 `<husk:SwitchContainer Value="{Binding Status}">` + 四个 `<husk:SwitchCase>` 按 `ScannerStatus` 枚举切换——不需要 `TargetType`，从绑定推断类型。**不**用多个 `IsVisible` bool 属性（那是 review 时识别出的冗余模式）。

**Rescan 行为**：从 Result/Empty 重新进入 Scanning，走完整 `i/N` 进度。扫描开始即清空旧结果，取消时回到 Idle（空集）——用户启动扫描即表明要新数据，旧结果已无法保障，不应继续展示。

**进度反馈**：Scanning 态标题旁挂一个 indeterminate `ProgressRing`，即使进度数值在胖 jar 解析期间停顿，spinner 持续转动表明在干活。

`Cancel` 通过 `CancellationTokenSource` 中断；`OnDeinitializeAsync` 兜底取消并 `_subscriptions.Dispose()`，避免 modal 关闭后扫描协程与 DynamicData 订阅泄漏。

### 3.6 搜索：DynamicData 声明式管道

用 `SourceList<HiddenModEntry>` 作数据源，`OnInitializeAsync` 里建一次管道，搜索文本变化时**自动重算过滤，零手动拷贝**：

```csharp
var text = this.WhenValueChanged(x => x.SearchText).Select(BuildTextFilter);
_source.Connect()
    .Filter(text)
    .Bind(out var view)
    .Subscribe()
    .DisposeWith(_subscriptions);
FilteredView = view;   // ReadOnlyObservableCollection<HiddenModEntry>?
```

`BuildTextFilter(string?)` 返回 `Func<HiddenModEntry, bool>`，空字符串 → 全收，否则对 `ModId` / `Name` 做大小写不敏感子串匹配。扫描完成后用 `_source.Edit(inner => { inner.Clear(); inner.AddRange(sorted); })` 原子喂数据，视图自动刷新。

> 与 `InstanceSetupPageModel` 的 `StageView` 管道同构（`WhenValueChanged` + `Filter` + `Bind` + `DisposeWith`）。

### 3.7 UI 结构：卡片列表（非表格）

每个 `HiddenModEntry` 渲染为一张 `HiddenModCard`（自定义 `ContentControl`，见 §5），信息分三行，不靠列对齐：

```
┌─ Jar In Jar Scanner ─────────────────────────── [×] ┐
│  [SwitchContainer 按 Status 切四态]                   │
│                                                       │
│  Idle:      图标 + 说明(扫描已部署模组) + [Scan]       │
│  Scanning:  ⟳ spinner + 进度条 + i/N + 文件名 + [Cancel]│
│  Empty:     图标 + 空文案 + [Rescan]                  │
│                                                       │
│  Result:                                              │
│  ◇ 25 jar files  ◆ 3 hidden mods  ⚠ 1 dup  [↻ Rescan]│
│  🔍 [........................................]        │
│  ┌──────────────────────────────────────────┐        │
│  │ sodiumextra              [Fabric][0.5.4] │  普通   │
│  │ Sodium Extra                             │        │
│  │ 📦 Embedded in sodium                    │        │
│  └──────────────────────────────────────────┘        │
│  ┌──────────────────────────────────────────┐        │
│  │ cloth-config             [Forge][11.0.3] │ ← 橙底  │
│  │ Cloth Config                             │ (Inner) │
│  │ 📦 Embedded in create                    │        │
│  └──────────────────────────────────────────┘        │
│  ┌──────────────────────────────────────────┐        │
│  │ sodium                  [Fabric][0.5.0]  │ ← 红底  │
│  │ Sodium                                   │(TopLv)  │
│  │ 📦 Embedded in create                    │        │
│  └──────────────────────────────────────────┘        │
└───────────────────────────────────────────────────────┘
```

卡片三行布局：
- **主行**：`ModId`（粗体）+ 右侧加载器/版本徽标（`husk:Tag`，`ObjectConverters.IsNotNull` 控制显隐）
- **Name 行**：次要色
- **宿主溯源行**：`BoxMultiple` 图标 + "Embedded in" + `Host`（粗体，核心信息）

**Idle 态说明文案**要点出"扫描的是**已部署到实例 mods 目录**的模组（部署产物，非 Profile 包清单）"——若刚改了包未重新部署，结果反映上次部署的产物。

样式遵循项目约定（AGENTS.md）：变体用 PascalCase class（如 `Primary`），状态用 lowercase 伪类（如 `:duplicate-inner`）。`HiddenModCard` 的两个危险态伪类是 `:duplicate-inner` / `:duplicate-toplevel`，对应 §3.4 的两种配色。

---

## 4. 数据模型

### 4.1 HiddenModEntry（`Models/HiddenModEntry.cs`）

```csharp
public sealed class HiddenModEntry
{
    public required string ModId { get; init; }
    public required string Name { get; init; }       // 缺省回退到 ModId
    public string? Version { get; init; }
    public ModLoaderKind? Loader { get; init; }      // 来自 AssetModeMetadataModel
    public required string Host { get; init; }       // 宿主 modId，缺省回退到文件名
    public DuplicateKind Duplicate { get; set; }     // 扫描后聚合填入，入列后不再变

    // DuplicateKind 嵌套在 HiddenModEntry 内——它是 HiddenModEntry 的专属概念
    public enum DuplicateKind { None, Inner, WithTopLevel }
}
```

> `DuplicateKind` 嵌套而非顶层，符合 AGENTS.md「按语义归属」：它只描述 `HiddenModEntry` 的状态。`Duplicate` 在入列前设定、之后不变，所以无需通知机制。

### 4.2 不新增 ModMetadata 类型

复用现有 `AssetModeMetadataModel`（`Models/AssetModeMetadataModel.cs`），不为本任务新建 parse 结果类型。

---

## 5. 改动面

| 层 | 文件 | 改动 |
|---|---|---|
| 解析（复用扩展） | `Utilities/AssetModHelper.cs` | 拆出 `ParseMetadata(ZipArchive)` 重载；新增 `EnumerateEmbeddedJars(ZipArchive)` + 两个私有 JSON 读取辅助（`ReadTopLevelJars` / `ReadQuiltJars`）；类注释补全 Quilt + 旧版 Forge |
| 卡片控件（新增） | `Controls/HiddenModCard.cs` + `Controls/HiddenModCard.axaml` | `ContentControl` + `Duplicate` 属性 + `:duplicate-inner` / `:duplicate-toplevel` 伪类；ControlTheme 仿 `DependencyGraphCard`（`CardBackgroundBrush` 底，两危险态切半透明 Warning/Danger 底 + 边框） |
| 控件注册 | `Themes/Controls.axaml` | 加 `<ResourceInclude Source="/Controls/HiddenModCard.axaml" />` |
| 模型 | `Models/HiddenModEntry.cs`（新增） | 内嵌 mod 视图项 + 嵌套 `DuplicateKind` 枚举 |
| ViewModel | `ModalModels/JarInJarScannerWidgetModalModel.cs`（新增） | 注入 `IViewContext<string>` 拿 Key；`ScannerStatus` 枚举状态机；Scan/Cancel 命令；扫描管线（§3.3）；重复检测（§3.4）；DynamicData 搜索管道（§3.6）；`OnDeinitializeAsync` 兜底取消 + 释放订阅 |
| 视图 | `Modals/JarInJarScannerWidgetModal.axaml`（改写） | `SwitchContainer` 四态 + Result 态汇总徽标 + 搜索条 + `HiddenModCard` 卡片清单 |
| 视图 code-behind | `Modals/JarInJarScannerWidgetModal.axaml.cs`（改写） | 移除 `Key` 属性与 `ScanAsync`（搬到 Model），回归纯 UI |
| 入口 | `Widgets/DeveloperToolboxWidget.axaml.cs`（改） | `new ... + PopModal(modal)` → `PopModal<JarInJarScannerWidgetModal>(Context.Key)`，走 activator |
| DEBUG 解除 | `Services/WidgetHostService.cs`（改） | 去掉 `#if DEBUG` 包裹，Release 也注册 |
| 本地化 | `Properties/Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs` | 新增 13 个键（见 §6），三文件同步 |
| 文档 | `plans/ADVANCED-CAPABILITY-ROADMAP.md` | 价值描述「类冲突」→「隐藏 mod 追溯」；标记本计划为独立蓝本 |

---

## 6. 本地化键

实际新增 13 个键（命名沿用 `InstanceDependencyGraphModal_xxx` 风格，三文件同步）：

```
JarInJarScannerModal_Title
JarInJarScannerModal_Description          // Idle 态说明（强调"已部署"）
JarInJarScannerModal_ScanButton
JarInJarScannerModal_ScanningTitle
JarInJarScannerModal_CancelButton
JarInJarScannerModal_EmptyTitle
JarInJarScannerModal_EmptyDescription     // 列三个原因：未部署/目录空/无内嵌
JarInJarScannerModal_RescanButton
JarInJarScannerModal_JarFilesLabel        // 汇总徽标
JarInJarScannerModal_HiddenModsLabel
JarInJarScannerModal_DuplicateLabel
JarInJarScannerModal_SearchPlaceholder
JarInJarScannerModal_EmbeddedInHeader     // 卡片宿主行的"Embedded in"前缀
```

> 不再有 `ModIdHeader` / `NameHeader` / `VersionHeader` 表头键——卡片列表不需要表头，信息在卡片内自说明。

---

## 7. 验收标准

| # | 场景 | 期望 |
|---|---|---|
| 1 | 打开 modal | Idle 态：说明 + Scan 按钮，不自动扫描 |
| 2 | 点 Scan | 进入 Scanning，spinner + `i/N` 进度 + 当前文件名，可 Cancel |
| 3 | mods 目录不存在 / 无 jar | 扫描结束 → Empty 态（文案列未部署/空/无内嵌） |
| 4 | 含 jar-in-jar 的实例 | Result 态：汇总徽标 + 卡片清单，每张卡有 ModId/Name/Version/Host |
| 5 | 宿主溯源行 | 显示宿主 modId（外层 parse 得到）或回退到文件名 |
| 6 | 纯库内嵌（无 modId） | 不出现在清单中 |
| 7 | 坏 jar | 跳过且不中断扫描，扫描正常完成 |
| 8 | 搜索框输入 | 实时过滤 modId/Name（DynamicData 管道自动重算） |
| 9 | 同 modId 被多个外层内嵌 | 卡片橙底（Inner） |
| 10 | 内嵌 modId 与顶层 modId 重复 | 卡片红底（WithTopLevel） |
| 11 | Rescan | 重新进入 Scanning 走完整进度，开始即清空旧结果 |
| 12 | 关闭 modal 时仍在扫描 | 不泄漏协程与订阅（OnDeinitialize 取消 + Dispose） |
| 13 | Release 构建 | DeveloperToolboxWidget 与 JarInJar 按钮可见（DEBUG-only 已解除） |
| 14 | 入口调用 | 走 `PopModal<TModal>(Key)` activator，配 Model |
| 15 | mods 目录含符号链接 jar | 能扫描到并正确解析（`Directory.GetFiles` 跟随符号链接） |
| 16 | 路径大小写与 entry 不符 | 不报（与加载器行为一致，§3.2） |

---

## 8. 风险与取舍

| 风险 | 取舍 |
|---|---|
| Quilt nested jar 格式不确定 | `ReadQuiltJars` 按 `quilt_loader.jars[].file` 推断实现，未实测验证；Quilt mod 本身的识别已由 `AssetModHelper` 覆盖 |
| 内嵌 jar 可能也是 jar-in-jar 宿主（三层嵌套） | 不递归，只 parse 一层；三层嵌套实际不存在 |
| 大型整合包（数百 jar）顺序扫描耗时 | jar 解压是毫秒级，数百 jar 总耗时仍在秒级；并行会破坏进度确定性与文件名反馈，不值 |
| 胖 jar（重度 shade）解析期间进度数值停顿 | Scanning 态常驻 indeterminate spinner，数值停顿时 spinner 仍转，表明在干活 |
| 路径大小写与 zip entry 不符 | 不归一化兜底——加载器也是大小写敏感的（已查证 §3.2），扫描器行为与之对齐才算正确 |

---

## 9. 备选方案备案

### 9.1 核心价值定位

当前选：**隐藏 mod 追溯**（§1.2）

| 备选 | 做法 | 否决理由 |
|---|---|---|
| 库层面多版本类冲突诊断 | 聚焦 gson/joml 等纯库的版本冲突 | 库不是 blame 目标——库出问题作者只能移除宿主 mod，且 jar-in-jar 内容绝大多数是库，冲突信号噪音过大；与「找到该移除谁」的真实诉求脱节 |

### 9.2 库的展示

当前选：**完全不显示**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| 默认折叠 + 「显示库」开关 | 保留库的可达性 | 用户明确「完全不关心库」，开关是多余的心智负担 |
| 独立 Libraries Tab | Mod/Library 分视图 | 同上，且 Tab 切换稀释了主清单的聚焦 |

### 9.3 内嵌 jar 的身份标识来源

当前选：**钻取内嵌 jar，读其自身元数据拿 modId**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| 用 Forge metadata.json 的 Maven `artifact` 当身份 | 不钻取，省一次 zip 打开 | `artifact ≠ modId`（create 内嵌的 artifact 可能叫 `create-framework`，modId 是 `create`）；且 Fabric 的 jars 字段只给文件路径，无坐标。钻取是拿真 modId 的唯一可靠途径 |

### 9.4 扫描触发时机

当前选：**Scan 按钮手动触发**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| 打开 modal 即自动扫描（对齐 InstanceDependencyGraphModal） | OnInitializeAsync 直接跑 | 该 modal 扫描是显式诊断动作，给用户「先看清标题再启动」的缓冲更合适；InstanceDependencyGraph 是被动展示已解析数据，语义不同 |

### 9.5 Rescan 的清空时机

当前选：**扫描开始即清空，取消回到 Idle 空集**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| 扫描期间保留旧结果，完成瞬间替换 | 避免扫描中界面闪空 | 用户启动扫描即表明要新数据，旧结果已无法保障，继续展示反而误导；取消得空集是符合预期的「没扫成」语义 |

### 9.6 搜索实现

当前选：**DynamicData `SourceList` + `WhenValueChanged().Select()` + `.Filter().Bind()`**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| ObservableCollection + LINQ 过滤重建 | 手动 Clear + Add 拷贝 | 手动拷贝是命令式样板，且项目已有 DynamicData 惯例（`InstanceSetupPageModel`）；声明式管道更短更不易错，搜索文本变化自动重算 |

### 9.7 视图维度

当前选：**单一「隐藏 mod 清单」（卡片列表）**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| By Container（按宿主分组）/ By Mod 双 Tab | 两个视角切换 | 核心场景是「找到某个 mod 的宿主」，单一清单 + 搜索已覆盖；By Container 视角用户极少需要，加 Tab 是噪音 |
| DataGrid 风四列表格 | MOD ID / NAME / VERSION / EMBEDDED IN 列对齐 | 列宽难调、长内容截断、容易歪扭；卡片分行信息密度更高且布局稳定 |

### 9.8 状态切换实现

当前选：**`SwitchContainer` 按 `ScannerStatus` 枚举切四态**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| 4 个 `IsVisible` bool 属性 + `OnStatusChanged` 通知 | 每个 bool 绑到对应面板的 IsVisible | 5 个属性 + 通知方法的状态管理样板；`SwitchContainer` 一行声明式搞定，且是项目既有惯例 |

### 9.9 重复类型的区分方式

当前选：**视觉颜色区分（Inner=Warning 橙，WithTopLevel=Danger 红）**

| 备选 | 做法 | 否决理由 |
|---|---|---|
| tooltip 区分两种重复 | 鼠标悬停看文字 | 软件设计原则是优先视觉差异，迫不得已才用 tooltip；颜色一眼可辨，符合"快速扫读找问题"的排查场景 |
| 砍成单一 `bool IsDuplicate` | 不区分 Inner/WithTopLevel | 与已装重复（真重复加载）比 jar-in-jar 间重复（selector 会仲裁）更严重，分级有诊断价值，不应抹平 |
