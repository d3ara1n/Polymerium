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
| **A1** | 依赖图增强：缺失依赖可视化 + 前置/被依赖 + 节点计数 | 🔴 P0 立即 | M | 待实施 |
| **A3** | Widget 开发者工具箱（补齐空壳 + 新增） | 🟠 P1 | M | 待定内容 |
| **B3** | 版本元数据源可配置 | 🟠 P1 立即 | S | 待实施 |
| **A2** | 查询语法升级为真正的查询引擎 | 🟡 P2 | L | 已确认方向 |
| **B1** | GitHub 整合包源（packwiz 仓库拉取/更新） | 🟡 P2 战略 | L | 待讨论 |
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
| DeveloperToolboxWidget | ⚠️ 仅 DEBUG，JarInJar Scanner 是空壳（只枚举文件无扫描逻辑），Git Commit Comparer 只有按钮无 command |

**问题**

`DeveloperToolboxWidget` 是专门为开发者/整合包作者准备的容器，但它停在「空壳」。用户反馈「除了 JarInJar 不知道还需要啥」。

**方案**

### 第一步：补齐 JarInJar Scanner（已规划）

实现真正的嵌套 jar 枚举：递归扫描实例 mods 下所有 `.jar`，对每个 jar 检测其内部 `META-INF/jarjar/` 或 `fabric.loader` 声明的内嵌依赖，报告：

- 哪些 mod 内嵌了哪些库
- 内嵌库的版本冲突（同一个库被多个 mod 以不同版本内嵌）
- 体积占比

**价值**：排查「明明没装 X 却报 X 的类冲突」这类 JarInJar 引发的疑难杂症，这是当前任何启动器都不提供的诊断。

### 第二步：候选 Widget（待用户挑选）

以下是开发者向 widget 的候选，按价值排序，供用户勾选真正需要的：

| 候选 | 价值 | 说明 |
|------|------|------|
| **启动耗时分析** | 高 | 记录每次启动各阶段（resolve/deploy/download/launch）耗时，趋势可视化。定位「为什么我的整合包启动要 3 分钟」 |
| **配置差异对比** | 高 | 对比两个实例的 Profile（包清单/规则/loader），高亮差异。维护多版本整合包时刚需 |
| **Crash 日志分析** | 中 | 解析 crash report / latestlog，识别报错的 mod 并关联到已安装包 |
| **模组加载顺序导出** | 中 | 解析游戏输出的实际 mod 加载顺序，与预期对比 |
| **运行时资源监控** | 中 | 游戏运行时的内存/CPU/线程数采样 |
| **依赖图快捷入口** | 低 | 把 A1 的依赖图作为可固定 widget 而非 modal |

**决策点**

请勾选第二步中真正想要的 widget（JarInJar 是确定的，其余需用户确认）。建议优先「启动耗时分析」——它对整合包作者的反馈最直接，且数据源（启动流程已有 progress 回调）现成。

---

## 🟠 B3 — 版本元数据源可配置

**现状**

`submodules/Trident.Net/.../PrismLauncherService.cs` 硬编码：

```csharp
public const string ENDPOINT = "https://meta.prismlauncher.org";
```

用户确认当前用的就是 Prism 的 meta 源。但它是**硬编码常量**，无法切换。

**目标**

把元数据源端点变成可配置项，默认仍为 `meta.prismlauncher.org`，允许用户填入镜像（如国内 BMCLAPI 兼容源、自建内网 meta）或社区维护的同类服务。

**方案**

1. `Configuration` 增加配置项 `ApplicationMetadataEndpoint`，默认 `https://meta.prismlauncher.org`
2. `PrismLauncherService` 把 `ENDPOINT` 常量改为从 `IOptions<Configuration>` 读取（submodule 改动，AGENTS.md 允许自由改 submodule）
3. 设置页「网络」或新增「数据源」分组加一个 Endpoint 输入框，带「重启生效」提示（参考现有代理设置的 `RestartRequiredLabelText`）

**涉及文件**

- `submodules/Trident.Net/.../PrismLauncherService.cs`
- `submodules/Trident.Net/.../ServiceCollectionExtensions.cs`（注入配置）
- `Configuration.cs` + `Properties/Resources.*`
- `Pages/SettingsPage.axaml` + `PageModels/SettingsPageModel.cs`

**决策点**

小。主要确认配置项放「网络」分组还是独立「数据源」分组。

---

## 🟡 A2 — 查询语法升级为查询引擎（已确认方向）

**现状**

`InstanceSetupPageModel.cs` 的搜索是一个内联 switch（`@Author`/`#Summary`/`!Id`），仅 AND、无引号、无 OR/NOT、无独立解析器。与功能强大的规则引擎（`RuleHelper`）完全割裂——规则引擎能表达 `And(Or(Purl(a), Purl(b)), Not(Tag(client)))`，但搜索框连「或」都做不到。

**目标**

让搜索框和规则引擎**共享同一套选择器语义**，最终可「把当前查询另存为规则」，形成闭环。

**方案方向（待细化）**

1. 抽取独立 `QueryParser`：解析 `@author:foo #tag:client modname` 这类表达式为 AST
2. 复用规则引擎的选择器（Tag/Kind/Repository/Purl）作为查询的 predicate
3. 支持组合：括号、OR（`|`）、NOT（`-`）、引号短语
4. 「另存为规则」：把当前查询一键转成 ProfileRule 注入规则引擎

**价值**

这是「高级用户会买单但当前启动器普遍缺失」的能力——PrismLauncher/Modrinth App 的搜索都只是朴素文本匹配。把声明式规则能力下放到搜索，是独占体验。

**状态**：方向已确认（「太对了，记下来」），因属 L 级重构，单独迭代，不混入 A1。

---

## 🟡 B1 — GitHub 整合包源（packwiz 仓库拉取/更新）

**现状**

导入/导出已支持 4 格式，但**导入是一次性的**——把 zip 解压进实例就结束。没有「这个实例绑定一个远端源，能 pull 更新」的能力。

用户愿景：**直接用 GitHub 当整合包源来更新**，典型场景是 packwiz 仓库（git-friendly TOML 元数据，社区整合包作者的事实标准）。

**目标**

实例可绑定一个 Git 仓库（packwiz 格式优先），支持：

- **首次拉取**：输入仓库 URL → clone → 解析 packwiz 的 `pack.toml` + `index.toml` → 构建实例（包清单进 Profile，文件进 import/live）
- **检查更新**：`git fetch` 比较 commit，提示「远端有新版本」
- **拉取更新**：`git pull` + 重新解析 → 增量更新实例的包清单（保留本地规则覆盖）

**为何这是战略级**

- 整合包作者用 packwiz + GitHub 发布已是主流工作流，但 packwiz 是**纯 CLI**，玩家侧要么手动装 packwiz-installer，要么作者导出成 mrpack 失去「持续更新」能力
- Polymerium 工作区（import/live + git 集成）+ 规则引擎在理念上**比 packwiz 更强大**（packwiz 没有规则引擎）。如果 Polymerium 能直接消费 packwiz 仓库，它就是「packwiz 整合包的最佳 GUI 前端」
- 这条路打通后，整合包作者这个核心用户群会主动迁移

**方案方向（待细化）**

1. 复用现有 `ImporterAgent` 抽象，新增 `PackwizRepositoryImporter`：clone → 解析 TOML → 转 Profile
2. 实例元数据增加「源仓库 URL + 当前 commit」字段
3. 工作区 git 集成已具备 fetch/pull 基建（LibGit2Sharp），扩展为「源同步」语义
4. 关键决策：packwiz 仓库的 mod 元数据（mod.pw.toml）描述的是「从哪下载」，需对接 `DataService` 下载实际 jar

**决策点**

- 是否同时支持「通用 Git 仓库（trident 格式）」与「packwiz 格式」，还是先只做 packwiz？
- 本地修改与远端更新冲突时策略（工作区已有 stage/restore 可借鉴）

**状态**：战略方向明确，需专门讨论细化方案后立项。

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
2. **B3 元数据源可配置** ← 顺手，S 级工作量
3. **A3 Widget** ← 用户勾选内容后启动，JarInJar 可先行
4. **B1 GitHub 整合包源** ← 战略级，专门讨论后立项
5. **A2 查询引擎 / B2 实例即工作流** ← B1 落地后自然演化
