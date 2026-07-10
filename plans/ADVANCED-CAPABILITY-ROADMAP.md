# Polymerium 高级能力路线图

> 制定日期：2026-06-23
> 定位：产品放弃「小白友好」红海竞争，转向**高级能力护城河**——面向整合包作者、硬核玩家、模组开发者。
> 与旧方向的关系：此前「降低专业门槛」一类条目（首页大按钮、隐藏高级功能、工具栏降密等）已全部驳回——与「高级能力」定位冲突，本计划是其替代方向。

## 现有能力盘点（地基）

经代码核查，以下能力**已经完整实现**，是后续路线的基建，不需重造：

| 能力 | 状态 | 意义 |
|------|------|------|
| **规则引擎 ProfileRules** | ✅ 完整 | 7 种选择器（Purl/Repository/Tag/Kind/And/Or/Not）× 3 种效果（Skipping/Normalizing/Destination），递归树形 UI。Minecraft 版的声明式包路由 |
| **工作区 InstanceWorkspace** | ✅ 完整 | import/live 双目录 + Git 集成（stage/restore/diff/commit/checkout） |
| **快照系统 Snapshots** | ✅ 完整 | 全量文件 + 包清单，去重存储，链式 diff，并行处理 |
| **导入/导出** | ✅ 完整 | 4 格式（trident/curseforge/modrinth/multimc），自动识别 |
| **账号系统** | ✅ 完整 | Microsoft / Yggdrasil（自定义验证服务器）/ 离线，多账户 |
| **Trident.Net 引擎** | ✅ 丰富 | 自定义启动参数、JVM 覆盖、三种启动模式、部署选项 |

以下能力是**半成品**，投入了 80% 工程量但停在「最后一公里」，是最高性价比的突破口：

| 能力 | 缺口 |
|------|------|
| **依赖图 DependencyGraph** | 只读可视化，不画未安装依赖、不显示被依赖、不标缺失 |
| **查询语法** | 3 个 token 的内联 switch，非解析器，与规则引擎割裂 |
| **Widget 系统** | 框架完整，仅 2 个实用 widget + 1 个空壳 |
| **Super Power 开关** | 名不副实，仅门控 Debug 模式 + 2 彩蛋 |

---

## 路线总览

| 编号 | 项目 | 优先级 | 工作量 | 状态 |
|------|------|--------|--------|------|
| **A1** | 依赖图增强：缺失依赖可视化 + 前置/被依赖 + 节点计数 | 🔴 P0 立即 | M | ✅ 已完成 |
| **A3** | Widget 开发者工具箱（补齐空壳 + 新增） | 🟠 P1 | M | 第一步已完成（JarInJar）；第二步经评估撚置（价值/ROI 低） |
| **B3** | 版本元数据源可配置 | 🟠 P1 立即 | S | 已驳回（仅有一个可用端点，可配置无意义，且用户误改会导致元数据不可用） |
| **A2** | 查询语法升级为真正的查询引擎 | 🟡 P2 | L | 已驳回 |
| **B1** | GitHub 整合包源（packwiz 仓库拉取/更新） | 🟡 P2 战略 | L | [PACKWIZ-REPOSITORY-IMPORT](PACKWIZ-REPOSITORY-IMPORT.md) |
| **B2** | 实例即工作流（配置即代码） | 🔵 P3 战略 | XL | 待讨论 |

---

## 🔴 A1 — 依赖图增强

**现状问题**

依赖图（`InstanceDependencyGraphModal`）目前是「好看的只读图」，但对整合包作者最有价值的两个诊断能力缺失：

1. **看不到缺失依赖**：`BuildGraphAsync` 只在 `nodes.TryGetValue(depKey, out var child)` 命中时才加边——即「只有已安装的依赖才出现在图里」。一个模组声明了强依赖 B 但 B 没装，图上完全无体现，而这正是整合包崩溃的最常见原因。
2. **只能看出边，看不出入边**：选中节点只列出「它依赖谁」，看不到「谁依赖它」（前置/被依赖）。排查「我能不能安全删掉这个 mod」时，被依赖关系才是关键。
3. **节点卡片没有计数**：无法一眼看出哪个节点是高扇入的「核心依赖」。

**目标**

- 缺失的 required 依赖以**错误色占位节点**进入图，直观表达「因它不存在导致强依赖断裂」
- 节点详情同时展示「依赖（出边）」与「被依赖（入边/前置）」
- 节点卡片显示依赖数 + 被依赖数

**方案**

### 1. 数据模型扩展

`Models/DependencyGraphNode.cs` 增加属性：

```csharp
public bool IsMissing { get; }      // 该依赖未安装（占位节点）
public int DependencyCount { get; } // 出边数：它依赖多少个
public int DependentCount { get; }  // 入边数：多少个依赖它
```

### 2. 构图逻辑改造（`ModalModels/InstanceDependencyGraphModalModel.cs`）

`BuildGraphAsync` 当前流程：取已安装包 → resolve → 只在已安装集合内连边。

改造为：

1. resolve 已安装包（不变）
2. 遍历每个包的 `pkg.Dependencies`，收集所有 `dep.IsRequired == true` 且 `depKey` 不在已安装集合中的标识
3. 对这批「缺失依赖标识」做一次 `ResolvePackagesAsync`，尽量拿到 ProjectName/Thumbnail/Author（让占位节点也有像样的铭牌）；resolve 不到的用 `ProjectId` 兜底
4. 为缺失依赖建 `DependencyGraphNode(IsMissing: true)`，用占位缩略图（如 `BoxDismiss` 图标）
5. 连边：parent → missingChild（与已安装边同样加，但目标 IsMissing）
6. 计算每个节点的 `DependencyCount`（出边）/ `DependentCount`（入边）

### 3. 选中节点详情改造

`SelectNode` 命令当前只填 `SelectedDependencies`（出边）。新增：

- `ObservableCollection<DependencyEntry> SelectedDependents`（入边/前置）
- 详情面板（axaml）在「依赖」分区下方加「被依赖（被 N 个模组依赖）」分区，复用同一套 `DependencyEntry` 模板

`DependencyEntry` 可选加 `IsMissing` 标记，用于在列表里也标出缺失项。

### 4. 节点卡片 + 样式

- `Controls/DependencyGraphCard.axaml`：加两个小计数徽章（↓依赖数 / ↑被依赖数），参考 `InstancePackageDependencyButton` 已有的 `RefCountTag` 样式
- `IsMissing` 节点：`DependencyGraphCard` 用 Danger 色（`ControlDangerForegroundBrush` / `ControlDangerBackgroundBrush`），缩略图区放 `BoxDismiss` / `Warning` 图标
- 统计卡片（图左下角）增加「缺失依赖」计数，用 Danger 色突出

### 5. 资源键

新增本地化键：`InstanceDependencyGraphModal_MissingTagText`、`InstanceDependencyGraphModal_DependentsTitle`（被依赖）、`InstanceDependencyGraphModal_MissingCountLabel`。

**涉及文件**

- `ModalModels/InstanceDependencyGraphModalModel.cs`（构图 + SelectNode）
- `Models/DependencyGraphNode.cs`（加属性）
- `Models/DependencyEntry.cs`（可选加 IsMissing）
- `Modals/InstanceDependencyGraphModal.axaml`（详情面板加被依赖分区 + 统计加缺失计数）
- `Controls/DependencyGraphCard.axaml`（计数徽章 + Danger 态）
- `Properties/Resources.{resx,zh-hans.resx,Designer.cs}`（3 键）

**决策点**

无（需求已明确）。唯一实现取舍：缺失依赖是否 resolve 元数据——**建议 resolve**（拿好名字），resolve 失败兜底 ProjectId。

---

## 🟠 A3 — Widget 开发者工具箱

**现状**

`Widgets/` 框架完整（`WidgetBase` 基类、Full/Slim 双模板、`WidgetHostService` 持久化），但内容极少：

| Widget | 状态 |
|--------|------|
| NoteWidget | ✅ 完整 |
| NetworkCheckerWidget | ✅ 完整 |
| DeveloperToolboxWidget | ✅ JarInJar Scanner 已完成、DEBUG 限制已解除；Git Commit Comparer 仍只有按钮无 command |

**问题**

`DeveloperToolboxWidget` 是专门为开发者/整合包作者准备的容器，但它停在「空壳」。用户反馈「除了 JarInJar 不知道还需要啥」。

**方案**

### 第一步：补齐 JarInJar Scanner ✅ 已完成并归档（蓝本：[archived/JARINJAR-SCANNER.md](archived/JARINJAR-SCANNER.md)）

扫描实例 mods 目录下所有 jar，钻取每个 jar-in-jar 嵌套的 mod（复用 `AssetModHelper` 的元数据解析），列出隐藏 mod 并追溯到宿主 mod，让整合包作者知道「游戏内多出来的 mod 是谁带来的、该移除谁」。支持按 modId/Name 搜索，并对重复（内嵌之间、内嵌 vs 顶层）高亮。

**价值**：排查「游戏内 mod 菜单比 mods 目录多出几个 mod，其中某个导致报错」的场景——暴露 jar-in-jar 隐藏 mod 并追溯到宿主，这是当前任何启动器都不提供的诊断。

### 第二步：候选 Widget（待用户挑选）

以下是开发者向 widget 的候选，按价值排序，供用户勾选真正需要的：

| 候选 | 价值 | 说明 |
|------|------|------|
| 启动耗时分析 | 低（已撚置） | 评估后不实施：数据需引擎插桩（非现成）、ROI 低、语义属统计板块（详见决策点） |
| **配置差异对比** | 高 | 对比两个实例的 Profile（包清单/规则/loader），高亮差异。维护多版本整合包时刚需 |
| **Crash 日志分析** | 中 | 解析 crash report / latestlog，识别报错的 mod 并关联到已安装包 |
| **模组加载顺序导出** | 中 | 解析游戏输出的实际 mod 加载顺序，与预期对比 |
| **运行时资源监控** | 中 | 游戏运行时的内存/CPU/线程数采样 |
| **依赖图快捷入口** | 低 | 把 A1 的依赖图作为可固定 widget 而非 modal |

**决策点（已评估 · 2026-07-06：撚置）**

「启动耗时分析」经详细调研后**不实施**：
1. **数据非现成**——此前判断「启动流程已有 progress 回调」有误；现有 `Activity` 表只记游戏会话时长（launch 起 → 进程退出，不含 deploy），要拿启动耗时必须在 Trident.Net 的 `DeployCoreAsync` / `LaunchCoreAsync` 插桩记阶段时间戳。
2. **ROI 低**——即便做出趋势，定位出的信息（下载慢 vs JVM 慢）对多数用户意义有限，不值得引擎侧插桩 + 新实体 + 图表库的投入。
3. **归属错位**——它语义上属「实例统计/活动」板块，而非开发者工具箱。

其余候选（配置差异对比、Crash 日志分析、模组加载顺序、运行时资源监控、依赖图快捷入口）暂无明确需求，整体撚置。DeveloperToolboxWidget 当前仅留 JarInJar 一个实用工具 + 半成品 Git Commit Comparer。

---

## 🟠 B3 — 版本元数据源可配置（已驳回）

**决策**：驳回。当前仅 `meta.prismlauncher.org` 一个可用端点，开放配置无实际价值，且用户误改会导致版本/加载器数据完全不可用。

## 🟡 A2 — 查询语法升级为查询引擎（已驳回）

**决策**：驳回。

| 理由 | 说明 |
|------|------|
| 场景错位 | 搜索是即时筛选，规则是声明式配置，两套场景对语法的需求完全不同。强行统一要么冗余、要么用户门槛上升 |
| 「另存为规则」伪需求 | 搜索时输入的临时条件存成规则没有意义——真要规则时用户直接在规则编辑器配就好了 |
| 现有一套够用 | 现有 `@`/`#`/`!` 前缀 + 包名模糊匹配覆盖了 90% 搜包场景，ROI 不值得 L 级重构 |
| 规则引擎与搜索各有其位 | 规则引擎有 And/Or/Not 组合能力，适合「写一次一直用」的声明式场景；搜索需要「打字即见结果」的即时性。两者不应对齐 |

---

## 🟡 B1 — GitHub 整合包源（packwiz 仓库拉取/更新）

**决策**：方案已定稿，见 [PACKWIZ-REPOSITORY-IMPORT](PACKWIZ-REPOSITORY-IMPORT.md)。

核心思路：新增 `PackwizRepository`（隐藏 IRepository）和 `PackwizImporter`（IProfileImporter），以 `pref://packwiz/owner/repo@tag` 为 URI。仓库整体作为 modpack 处理，mod 仅通过 `.pw.toml` 的 `[update]` 块提取外部引用，由现有 DataService 链路 resolve。

---

## 🔵 B2 — 实例即工作流（配置即代码）详解

> 用户要求「细说」此项。

**核心理念**

把一个实例从「一堆运行时文件 + 内部二进制 Profile」变成**一个可版本控制、可审查、可协作的工作产物**。类比：dotfiles 之于个人配置，Infrastructure-as-Code 之于运维——这里是 **Instance-as-Code**。

**现状离这个理念有多近**

已经具备 80% 基建：

- ✅ 工作区有 git 集成（commit/checkout/diff）
- ✅ import/live 双目录分离（源 vs 运行时，天然对应 git 的 index/working tree）
- ✅ 规则引擎是声明式的（已经是「配置」而非「命令式代码」）
- ✅ 快照系统记录全量状态
- ❌ **Profile 不是文件**——它是内部模型 + SQLite，不可读、不可 diff、不在 git 管辖内

**缺的最后一步：Profile 文件化**

把 Profile（包清单、规则、loader/版本、JVM 参数）序列化为**人类可读文本**（TOML/JSON），放在工作区目录里纳入 git。

一旦 Profile 变成文件，「实例即工作流」的全部能力随之解锁：

| 能力 | 说明 |
|------|------|
| **可审查变更** | 装了个 mod、改了条规则、升了版本 → `git diff` 一目了然，`git log` 知道何时何人改的 |
| **可协作** | 多人搭整合包：分支开发 + PR review 配置变更，和写代码一样 |
| **可模板化** | 一个仓库参数化生成多版本实例（同一整合包的 1.20.1 / 1.21 分支） |
| **CI 验证** | 仓库推送后，CI 用 Trident CLI（已存在）部署验证，集成包作者能在发布前发现冲突 |
| **与 packwiz 生态对齐** | B1 拉取的 packwiz 仓库本身就是「实例即代码」，Profile 文件化让 Polymerium 原生具备同构能力，互通零摩擦 |

**与各能力的协同**

- 与 **B1**：Profile 文件化是「导出为 packwiz / 从 packwiz 同步」的天然中间层
- 与 **规则引擎**：规则本来就是声明式，序列化为 TOML 最自然
- 与 **快照**：快照记录二进制全量，Profile 文件记录可读增量，两者互补（快照=时间机，Profile 文件=版本控制）
- 与 **Super Power**：可重新定义 Super Power 为「实例即代码工作流」开关——开启后 Profile 以文件形式落盘并纳入 git，关闭则维持内部模型（对不需要这套工作流的玩家无打扰）

**方案方向（粗粒度）**

1. 设计 Profile 序列化格式（TOML，对齐 packwiz 风格降低学习成本）
2. `ProfileManager` 增加「文件双向同步」：内存 Profile ↔ 工作区 `profile.toml`
3. 工作区 git 集成扩展：commit 时自动包含 `profile.toml`
4. 设置项（Super Power 门控）：开启「实例即代码」模式

**状态**：属 XL 级，依赖 B1 的理念先落地（packwiz 格式本身就是 Profile 文件化的参考实现）。建议 B1 推进后再以 packwiz 互操作为切入点自然演化出 B2，而非独立巨石式开发。

**决策点（待讨论）**

- 序列化格式：TOML（生态对齐 packwiz）vs JSON（实现简单）vs 自定义
- 是否真的需要 Super Power 门控，还是直接作为默认行为
- 与现有 SQLite 持久化的关系（文件为主、DB 为缓存？还是并行？）

---

## 实施建议顺序

1. **A1 依赖图增强** ← 现在，需求明确、风险低、最能立刻体现「专业工具」定位
2. **A3 Widget** ← 用户勾选内容后启动，JarInJar 可先行
3. **B1 GitHub 整合包源** ← 战略级，专门讨论后立项
4. **B2 实例即工作流** ← B1 落地后自然演化
