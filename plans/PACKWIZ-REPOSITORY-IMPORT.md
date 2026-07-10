# Packwiz 仓库导入：以 Git 仓库为持续数据源

> 制定日期：2026-07-10
> 定位：战略级能力。将 packwiz 格式的 GitHub 仓库作为一等公民数据源，实例可绑定远端仓库并持续同步更新，消除「导入一次即结束」的断层。
> 当前状态：未实施
> Jira：[POLY-125](https://d3ara1n.atlassian.net/browse/POLY-125)

---

## 背景与动机

### 现状

当前导入流程是**一次性的**——用户拖入 `.mrpack` / `zip`，解压实例包，之后与源再无联系。实例没有「绑定远端」的概念，更新只能靠作者重新发布 zip、用户重新导入。

packwiz 是 Minecraft 整合包社区的事实标准格式，核心设计就是 **Git 友好的 TOML 文本目录**——作者在 GitHub 维护 `pack.toml` + `mods/*.pw.toml`，每个 `.pw.toml` 描述 mod 去哪下载、hash 是什么、更新源指向哪（Modrinth / CurseForge）。但 packwiz 本身是纯 CLI，玩家要么装 `packwiz-installer`，要么等作者导出 `.mrpack` 发布——后者再次回到「一次导入」的老路。

### 问题

- 整合包作者用 Git 维护整合包，但玩家侧的体验仍然是静态 zip
- 更新流程断裂：作者改一行 `mods/*.pw.toml` 里的版本号 → 玩家不知道、收不到推送
- Polymerium 已有工作区 git 集成（LibGit2Sharp）、规则引擎、4 种导入格式，唯独缺少「把 Git 仓库本身当数据源」的能力

### 机会

- packwiz 格式天然适合作为多游戏引擎的 modpack 元数据层——它不仅描述 mod 下载，还描述 loader、版本、侧（client/server）
- 打通这条路后，整合包作者可以用 Polymerium 作为 packwiz 的 GUI 前端，实现「packwiz 仓库 → 实例 → 持续同步」闭环
- 后续多游戏架构（Hytale/Mindustry）的 modpack 导入也可复用 packwiz 格式的目录结构约定，无需重新设计

---

## 目标 / 非目标 / 不做的事

**目标**

1. 新增 URI scheme `pref://packwiz/owner/repo[@tag]`，作为 packwiz 仓库的统一标识
2. 实现 `PackwizRepository`（隐藏 `IRepository`），只支持 `Resolve` 和 `Query`——将整个 packwiz 仓库解析为 `Kind=Modpack` 的 Project/Package，下载地址为 GitHub 对应 tag/commit 的 archive zip
3. 实现 `PackwizImporter`（`IProfileImporter`），接收仓库 zip 后扫描 `mods/*.pw.toml` 等元数据文件，提取外部包引用（pref）列表供下游 resolve
4. Importer 同时将非元数据文件（`config/`、`scripts/` 等）按相对路径复制到实例的正确位置
5. 后续支持实例绑定远端仓库 URL，`git fetch/pull` 检查更新并重新导入

**非目标**

- 不实现 packwiz 仓库的搜索和枚举（`SearchAsync`/`InspectAsync` 等方法抛不支持）
- 不实现 packwiz 仓库内单个 mod 的 resolve（mod 只通过 `[update]` 块的外部引用间接 resolve）
- 不实现通用 Git 仓库支持——仅 packwiz 格式
- 不实现 packwiz 仓库的写入/导出（从 Polymerium 反向导出为 packwiz 格式）

**不做的事**

- **不直接下载 mod JAR**——PackwizImporter 只提取引用，不自行下载。JAR 下载由现有的 DataService + ModrinthRepository / CurseForgeRepository 完成
- **不修改 packwiz 规范**——消费标准格式，不引入 Polymerium 专有扩展字段
- **不替代表现有的 zip 导入流程**——两者并行，packwiz 仓库是额外数据源，不是替代

---

## 关键决策与取舍

### 决策 1：仓库作为 modpack，mod 只产引用

PackwizRepository 不感知仓库内的单个 mod。它的 Resolve 对象是**整个 modpack**（Kind=Modpack），下载地址是 GitHub 对应 tag/commit 的 archive zip。单个 mod 的 resolve 由 PackwizImporter 扫描 `.pw.toml` 后提取的 `[update]` 块（Modrinth mod-id / CurseForge project-id）委托给现有仓库完成。

**理由**：避免在 hidden IRepository 内部重复实现 mod 级信息聚合。packwiz 元数据缺少 Author/Summary/Thumbnail 等字段，硬填 Project/Package 记录反而制造残缺数据。

### 决策 2：vid（版本标识）为 Git tag 或 commit SHA

- `pref://packwiz/owner/repo@v1.0.0` → tag
- `pref://packwiz/owner/repo@abc1234` → commit
- 无 vid 时取默认分支最新 commit

下载 URL 对应 GitHub archive API。

### 决策 3：PackwizRepository 为隐藏 IRepository

不注册到仓库选择器/搜索列表。不实现 Search / Identify / ReadDescription / ReadChangelog / Inspect。仅 Resolve 和 Query 可正常工作。URI 的解析由 PackwizRepository 自行处理，用户通过显式的 `pref://packwiz/...` 引用触发。

### 决策 4：从 packwiz 目录结构到实例文件的映射规则

| packwiz 目录内容 | 实例映射 |
|------------------|----------|
| `mods/*.pw.toml` | 不映射为文件——提取外部包引用，转给 DataService 下载 |
| `resourcepacks/*.pw.toml` | 同上 |
| `shaderpacks/*.pw.toml` | 同上 |
| 其余文件（`config/`、`scripts/` 等） | 按相对路径直接复制到实例的 override 目录 |

### 决策 5：pack.toml 缺失字段的处理

`pack.toml` 只保证提供 name、author、version（可选）、versions（minecraft + loader）。Summary、Thumbnail、Tags、DownloadCount、Gallery 等 `Project` 所需字段**不存**。Resolve 结果中这些字段留空或填合理默认值。

---

## 影响面

| 领域 | 影响 |
|------|------|
| 包标识解析层（Pref） | 新增 `pref://packwiz/owner/repo[@tag]` 格式的解析与验证 |
| 仓库抽象层（IRepository） | 新增 PackwizRepository 实现，隐藏不注册 |
| 导入引擎（IProfileImporter / ImporterAgent） | 新增 PackwizImporter：接受 zip 目录而非压缩包，扫描 `.pw.toml` 提取引用 |
| 实例元数据 | 增加「源仓库 URL + 当前 commit/tag」字段（后续更新检查用） |
| 外部服务（DataService） | 无改动——mod 引用走现有 resolve 链路 |
| 工作区 git 集成 | 后续可将 git fetch/pull 扩展为源同步语义 |

---

## 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | Resolve `pref://packwiz/comp500/packwiz-example-pack@v1.0.0` | 返回 Kind=Modpack 的 Package，ProjectName 来自 pack.toml name，Download 指向 GH archive zip |
| 2 | Query 同一个 pref | 返回 Project，含 author、name、minecraft version、loader |
| 3 | Search 同一个 pref | 抛出 NotSupportedException |
| 4 | PackwizImporter 接收 GH archive zip | 扫描 mods/*.pw.toml，提取 `[update.modrinth]` / `[update.curseforge]` 为外部 package ref |
| 5 | 非元数据文件复制 | zip 内的 `config/`、`scripts/` 等文件按相对路径复制到实例 override 目录 |
| 6 | 无 `[update]` 块的 mod | 跳过，不产生 package ref（用户需手动添加该 mod） |
| 7 | `pack.toml` 只含必填字段 | Resolve 正常，缺失字段填默认值 |
| 8 | 实例绑定仓库后检查更新 | `git ls-remote` 比较远端 tag 与本地 commit，提示有新版本 |

---

## 备选方案备案

### A. 仓库即包源，而非中转站

**备选**：PackwizRepository 直接 resolve 仓库内每个 `.pw.toml` 为 Package，不经过 importer。

**否决理由**：packwiz 仓库是 modpack 级别的概念，不应承担 mod 级仓库的职责。mod 的下载/版本信息在 `.pw.toml` 的 `[update]` 块中已指向 Modrinth/CF，绕过它们去重新聚合是冗余。且 packwiz 元数据缺少 Project/Package 需要的多个字段，产出的记录残缺。

### B. 同时支持通用 Git 仓库

**备选**：不仅 packwiz，任何 Git 仓库都可作为实例源。

**否决理由**：packwiz 有明确格式约定（`pack.toml` + `index.toml` + `mods/*.pw.toml`），通用 Git 仓库无此约定。先只做 packwiz，后续若有自定义格式需求再扩展。

### C. Importer 内嵌 mod JAR 下载

**备选**：PackwizImporter 直接根据 `.pw.toml` 的 `download.url` 下载 JAR，不经过 DataService 的 resolve 链路。

**否决理由**：需要重复实现 Modrinth/CF API 的版本匹配逻辑（loader/游戏版本筛选），且错过现有缓存、hash 验证、重试等基础设施。不如产出一份 pref 列表，让 DataService 统一处理。
