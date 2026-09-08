# Mindustry 接入计划：在多游戏地基上加第三个游戏

> 制定日期：2026-07-08
> 定位：评估接入 Mindustry（Anuken 的开源工厂/塔防游戏）客户端的复用边界与专属架构挑战。**本计划不重新设计多游戏架构**——HYTALE-INTEGRATION.md 已完成该设计，本计划假定它作为地基存在，Mindustry 是挂在这套地基上的下一个游戏实现。
> 当前状态：**草案**。依赖多游戏地基是否落地、实例隔离方案未定、是否立项未决，不可照此施工。

---

## 1. 与 Hytale 计划的关系（前置前提）

[HYTALE-INTEGRATION.md](HYTALE-INTEGRATION.md) 已设计完整的多游戏改造方案，核心是一组按游戏分发的多态抽象：

- `GameKind` 一等公民 + `Profile.Rice.Game` 字段（§5.1–5.2）
- 部署产物 `ILockPayload` 多态（§5.3）
- 部署 `IDeployPlanner`、启动 `ILaunchStrategy`、运行时 `IRuntimeResolver` 三套按游戏分发的策略（§5.4–5.6）
- 模组来源 `IRepository`、modpack 导入 `IProfileImporter`、账号 `IAccount` 三套框架复用 + 各游戏自配实现（§5.7–5.9）
- 配置版面按游戏类型多态的 schema 机制（§5.11）

本计划的复用判断因此分**三类来源**：

| 来源 | 含义 |
|------|------|
| 继承**现有 Minecraft 架构** | 与游戏无关的通用层，Mindustry 直接用 |
| 继承 **Hytale 地基** | 多游戏抽象本身，P0 落地后所有游戏共享 |
| Mindustry **专属新建** | Mindustry 独有的实现与挑战 |

**关键时序判断**：若 Hytale 的 P0 地基尚未落地，Mindustry 接入需先（或协同）完成该地基；若已落地，Mindustry 是轻量接入。Mindustry 也可作为推动多游戏地基落地的契机——它比 Hytale 简单得多（无 OAuth、无签名、无增量补丁、模组即拖即用），是更低风险的"第一个新游戏"试金石。

---

## 2. 背景与动机

- **管理体验原始，有启动器化空间**：Mindustry 客户端目前手动下 JAR、手动放 mod、版本切换靠重下重装，没有实例隔离、版本管理、模组仓库的概念。
- **接入门槛低于 Hytale**：模组源（`Anuken/MindustryMods` 的 `mods.json`）完全公开无认证；版本源（GitHub Releases）可轮询；客户端无账号系统、无启动校验、离线即玩。
- **物理形态是 JVM 游戏**（`java -jar`），与 Minecraft 同类、与 Hytale（原生 exe）异类——这点直接决定 Mindustry 的启动实现取向接近 Minecraft 而非 Hytale。

---

## 3. 复用边界：逐层判断

下表沿用 HYTALE-INTEGRATION §2.2 的判断框架，把 Mindustry 作为第三列填入。判定依据始终是同一把尺子：**这层面对的是"游戏产物的物理形态"，还是"实例怎么管、文件怎么放"？前者必须多态，后者共享。**

| 架构层 | 面对什么 | Minecraft | Hytale | Mindustry | Mindustry 复用判断 |
|--------|----------|-----------|--------|-----------|--------------------|
| 实例目录结构 / `PathDef` | 通用容器 | 用 | 用 | 用 | ✅ 继承现有，零改动 |
| `Profile.Rice` 模型 | 通用容器（+`Game`） | 用 | 用 | 用 | ✅ 继承 Hytale 地基（`Game=Mindustry`） |
| persist / import / live / snapshot 目录机制 | 通用文件放置 | 用 | 用 | 用 | ✅ 继承现有，零改动 |
| 实例生命周期（`InstanceManager`） | 通用协调器 | 用 | 用 | 用 | ✅ 继承现有 |
| 内容寻址快照 `SnapshotManager` | 通用去重存储 | 用 | 用 | 用 | ✅ 继承现有——版本 JAR / 模组文件去重 |
| 符号链接落盘 `SolidifyManifestStage` | 通用文件隔离 | 用 | 用 | **关键复用** | ✅ 继承现有，**且是实例隔离的解法之一**（见 §4.1） |
| 模组来源 `IRepository` | 框架通用 | CurseForge/Modrinth | +HytaleCF(gameId=70216) | +`MindustryModsRepository`（拉 `mods.json`） | 🟡 继承框架，新建实现 |
| modpack 导入 `IProfileImporter` | 框架通用 | 4 种 MC 格式 | +Hytale importer | 无标准格式 | 🟡 框架在，Mindustry 可暂不做（生态早期） |
| 账号系统 `IAccount` | 框架通用 | 微软/盒子/离线 | +Hytale OAuth | **不需要** | 🟢 框架在，Mindustry 留空——这是相对 MC/Hytale 的**简化优势** |
| 部署产物（`ILockPayload`） | **游戏产物物理形态** | `MinecraftPayload` | `HytalePayload` | `MindustryPayload` | 🔴 新建（轻：JAR 路径 + JVM 参数 + 数据目录） |
| 部署 stage（`IDeployPlanner`） | **游戏怎么装** | MC 8-stage | Hytale 组合 | Mindustry 组合（下 JAR + 建目录 + 隔离 hack） | 🔴 新建（极薄） |
| 启动引擎（`ILaunchStrategy`） | **游戏怎么跑** | java + MC 令牌 | 原生 exe | `java -jar` + 数据目录 hack | 🔴 新建（取向接近 MC，但要处理隔离） |
| 运行时来源（`IRuntimeResolver`） | **JRE 哪来** | Mojang Piston | Temurin 25 @ hytale.json | Temurin 17 / 系统 Java | 🔴 新建 |
| 配置版面 schema | 按游戏多态 | JVM 四项 | Hytale 项 | JVM 相关项 | 🟡 继承 Hytale §5.11 机制，加 Mindustry schema |
| 日志解析 | 游戏专属格式 | MC `Scrap` | (Hytale) | Mindustry 行式 | 🔴 新建（简单） |

**读法**：✅ 直接继承零改动；🟢 继承且对 Mindustry 是简化；🟡 继承框架需补实现；🔴 Mindustry 专属新建（但得益于地基，每个都是薄实现）。

**核心洞察**：必须新建的层（产物 / stage / 启动 / 运行时）和 Hytale 计划的判断完全一致——这些都是"游戏物理形态"层，任何游戏都得各写一套。差别在于：Mindustry 这四层都比 Hytale 轻得多（无 OAuth、无签名、无资源包、无增量补丁），启动取向还和 Minecraft 同源（JVM）。

---

## 4. Mindustry 专属架构挑战

### 4.1 实例隔离（命门，也是与 Hytale 的根本差异）

Hytale 有干净的 `--user-dir` 参数把用户数据目录指到任意路径，persist 机制天然契合。**Mindustry 没有这个能力**——Arc 框架把数据目录硬编码在 `OS.getAppDataDirectoryString()`，无 CLI 参数、无环境变量可重定向（源码核查：`Anuken/Arc` 的 `OS.java`、`Mindustry` 的 `DesktopLauncher.java` 只解析 width/height/gl/debug 等，无 `--data`）。

这是 Mindustry 独有的架构负担，MC 和 Hytale 都不存在。解法见 §6 备选方案。`SolidifyManifestStage` 的符号链接能力恰好能在这里复用，但带平台前提（Windows 需 Developer Mode——Polymerium 的 `OozePrivilege` 已在测符号链接，基础设施现成）。

### 4.2 版本源：GitHub Releases 轮询，无官方列表 API

- 稳定版：`Anuken/Mindustry` releases，tag `v{build}`（如 `v146`）。
- Bleeding Edge：独立仓库 `Anuken/MindustryBuilds` releases，产物 `Mindustry-BE-{build}.jar`。
- 无官方版本枚举端点，只能轮询 GitHub Releases API（受 rate limit 约束，但源只有两个，量小）。

新建一个轻量版本源服务，对接进 `IRuntimeResolver` 之外的版本选择 UI。这块 Mindustry 自己解决，不复用 `PrismLauncherService`。

### 4.3 模组源：GitHub 自动索引清单，即拖即用

- 不是 CurseForge/Modrinth 那样的托管平台，更不是商店——游戏内 Mod Browser 拉的是 `Anuken/MindustryMods/mods.json`（+ jsdelivr CDN 镜像），完全公开，字段完整（repo / name / author / minGameVersion / hasJava / hasScripts / stars / lastUpdated）。
- **收录是自动的、无审核**：GitHub 上带 `mindustry-mod` topic 且有合法 `mod.json` 的仓库，被脚本每 2 小时扫描收录进清单；作者无需上传、不经审核，打上 topic 即进。想退出可在 `mod.json` 加 `hideBrowser: true`。下载直接走该模组 GitHub 仓库的 `archive/master.zip`。
- 模组无加载器概念，`.jar`/`.zip` 丢进 `mods/` 即用；有简单的硬/软依赖解析（拓扑排序 + 循环检测），但远比 MC 的 Fabric/Forge 体系简单，Trident 的包依赖仲裁对它是过度设计。
- 无审核 + `hasJava`/`hasScripts` 模组可直接进清单，任意代码执行风险实在，模组仓库 UI 应予提示。

新建 `MindustryModsRepository`（`IRepository` 实现，拉 `mods.json` + GitHub `archive` 下载）。

### 4.4 账号：缺失即优势

Mindustry 客户端无账号系统、离线即玩。`IAccount` 框架在，但 Mindustry 实例**整个跳过账号配置环节**——这是它相对 MC（微软/Xbox/Yggdrasil）和 Hytale（OAuth Device Flow）的显著简化，少一整套认证链实现与 UI。

---

## 5. 改动面（架构级）

按"继承 / 新建 / 改造"分组，给到目录/文件级，不给行号（架构评估，非施工清单）。

### 完全继承（零改动）

- 应用壳子：`Services/NavigationService`、`OverlayService`、`ConfigurationService`、`PersistenceService`、`Facilities/*`（`ViewModelBase`、`SimpleViewActivator`、`SimpleViewStatePersistence`）、`MainWindow`、主题、通知
- Trident 通用层：`SnapshotManager`、`SolidifyManifestStage`、`PathDef` 的 import/persist/live/snapshot 目录定义

### 继承 Hytale 地基（P0 落地后所有游戏共享）

- `GameKind`（加 `Mindustry` 值）、`Profile.Rice.Game` 字段
- `ILockPayload` / `IDeployPlanner` / `ILaunchStrategy` / `IRuntimeResolver` 四套多态抽象
- `IRepository` / `IProfileImporter` / `IAccount` 框架
- 配置 schema 多态机制（HYTALE-INTEGRATION §5.11）

### Mindustry 专属新建

- `MindustryPayload`（`ILockPayload` 实现）
- `MindustryDeployPlanner`（`IDeployPlanner` 实现，极薄）
- `MindustryLaunchStrategy`（`ILaunchStrategy` 实现，含数据目录隔离 hack）
- `MindustryRuntimeResolver`（`IRuntimeResolver` 实现，Temurin 17 / 系统 Java）
- `MindustryModsRepository`（`IRepository` 实现，拉 `mods.json`）
- Mindustry 版本源服务（GitHub Releases 轮询，stable + BE）
- Mindustry 配置 schema + 日志解析

### 改造

- `InstanceService` / `RepositoryAgent` / 启动分发按 `GameKind` 多路分发（HyTale 地基的一部分，Mindustry 复用既有改造）
- 实例创建 / 配置 UI 支持 Mindustry 实例类型

---

## 6. 备选方案备案：实例隔离

实例隔离是本计划的核心不确定性。已评估的方案：

| 方案 | 描述 | 判定 |
|------|------|------|
| **符号链接数据目录** | 把 `~/<数据目录>/Mindustry/{mods,saves,...}` 软链到实例 persist 目录；复用 `SolidifyManifestStage` 能力 | ✅ 主选。但 Windows 需 Developer Mode（符号链接权限），跨平台行为需逐平台验证 |
| **`-Duser.home` 劫持** | 启动时设 JVM 系统属性 `-Duser.home=<实例目录>`，让 Arc 的 `userHome` 拼接落到实例 | ❌ 否决。副作用过大——会重定向所有 Java 路径（`~/.m2`、其他缓存），污染面不可控 |
| **Steam 模式路径机制** | 利用 Mindustry Steam 版把数据目录设为本地 `saves/` 的特殊路径 | ❌ 否决。仅 Steam 版生效，普适性差，且属于 hack |
| **接受单实例（不做隔离）** | Mindustry 实例共享一个数据目录，启动器只管版本/模组，不隔离存档 | ⚠️ 退路。功能降级，失去"实例"的核心价值，与 MC/Hytale 体验不对等 |
| **等上游加重定向参数** | 向 Mindustry 提 issue/PR 增加数据目录 CLI 参数 | ⚠️ 最干净但不可控、无时间表。可作为长期方向，不阻塞当前方案 |

**当前倾向**：符号链接方案主选，理由是 Polymerium 已有 `SolidifyManifestStage` 和 `OozePrivilege` 的符号链接基础设施，且 persist 机制语义天然匹配。Windows Developer Mode 前提与现有实例部署流程一致，不引入新约束。

---

## 7. 待确认问题

1. **立项与时序**：Mindustry 是否独立立项？若 Hytale 先行，Mindustry 作为地基上的第二个新游戏轻量接入；若想用 Mindustry 当多游戏地基的试金石（更低风险），则需调整 Hytale 计划的 P0–P2 时序。
2. **实例隔离方案**：符号链接（主选）的跨平台一致性如何保证？是否接受"无 Developer Mode 的 Windows 退化为单实例"的降级？
3. **版本范围**：是否同时支持 stable + Bleeding Edge？BE 仓库结构与稳定版不同，要两套版本源处理。
4. **Java 模组安全**：清单收录无审核，`hasJava` / `hasScripts` 模组有任意代码执行风险，是否在模组仓库 UI 做安全提示或默认过滤？
5. **运行时来源**：Mindustry 要求 JDK 17+。是像 Minecraft 那样托管下载 Temurin，还是直接用系统已装 Java？前者体验一致但多一套运行时管理，后者轻量但依赖用户自备。
6. **modpack**：Mindustry 无标准 modpack 格式，`IProfileImporter` 在 Mindustry 侧是否先不做？还是 Polymerium 自定义一个基于 `trident.index.json` 的格式？

---

## 8. 外部参考

- **Mindustry 仓库**：github.com/Anuken/Mindustry
- **BE 构建仓库**：github.com/Anuken/MindustryBuilds
- **模组清单源**：github.com/Anuken/MindustryMods（`mods.json`）
- **Arc 框架**（数据目录硬编码点）：github.com/Anuken/Arc `arc-core/src/arc/util/OS.java`
- **DesktopLauncher**（CLI 参数核查点）：github.com/Anuken/Mindustry `desktop/src/mindustry/desktop/DesktopLauncher.java`
- **既有第三方启动器先例**：github.com/BalaM314/MindustryLauncher（未实现真正实例隔离）
- **内部依据**：[HYTALE-INTEGRATION.md](HYTALE-INTEGRATION.md)（多游戏地基设计）
