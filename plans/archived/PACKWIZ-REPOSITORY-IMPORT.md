# Packwiz 仓库导入：把 GitHub 上的 packwiz 仓库作为实例数据源

> 制定日期：2026-07-10
> 定位：把 packwiz 格式的 GitHub 仓库接入为一等数据源——实例绑定远端仓库后，可通过 HTTP 检查并拉取更新，消除「导入一次即结束」的断层。
> 当前状态：未实施
> Jira：[POLY-125](https://d3ara1n.atlassian.net/browse/POLY-125)

---

## 背景与动机

当前导入流程是一次性的：用户拖入 `.mrpack` / `zip`，解压成实例，之后与源再无联系，更新只能靠作者重新发布包、用户重新导入。

packwiz 是 Minecraft 整合包社区的事实标准格式，核心设计就是 **Git 友好的 TOML 文本目录**——作者在 GitHub 维护 `pack.toml` + `mods/*.pw.toml`，每个 `.pw.toml` 声明 mod 去哪下载、hash 是什么、更新源指向哪（Modrinth / CurseForge）。但 packwiz 本身是纯 CLI，玩家要么装 `packwiz-installer`，要么等作者导出 `.mrpack`——后者再次回到「一次导入」的老路。

打通这条路后，整合包作者可以用 Polymerium 作为 packwiz 仓库的 GUI 前端，实现「仓库 → 实例 → 持续更新」闭环。

## 数据来源与传输方式

全程 **HTTP**，不涉及任何 Git 操作：

- 读元数据（`pack.toml` / 目录结构）：GitHub Contents/Commits REST API
- 检查更新：GitHub REST API 比对远端 commit/tag 与本地记录
- 下载整合包快照：GitHub codeload archive（`/archive/{sha}.zip`）

无 clone、无 fetch、无 libgit2 调用。

## 目标

1. 在 `IRepository` 上新增 `IsHidden` 属性；`RepositoryAgent.Labels` 及派生它的搜索/选择器 UI 过滤掉 `IsHidden == true` 的仓库。隐藏仓库不进任何枚举列表，但 `Resolve`/`Query` 等正常生效。
2. 新增 `PackwizRepository`（`IsHidden = true`）：把整个 packwiz 仓库解析为 `Kind = Modpack` 的 `Project` / `Package`，`Package.Download` 指向 GitHub archive zip。
3. 新增 URI scheme `pref://packwiz/owner/repo[@ref]`，`@ref` 为 Git tag、分支名或 commit SHA；缺省时取默认分支的最新 commit。
4. 新增 `PackwizImporter`（`IProfileImporter`，`IndexFileName = "pack.toml"`）：解析 `pack.toml` 得到 Minecraft 版本与 loader，扫描 `mods/`、`resourcepacks/`、`shaderpacks/` 下的 `*.pw.toml` 提取外部包引用，其余文件按相对路径复制到实例 import 目录。
5. 实例 `Setup.Source` 记录 `pref://packwiz/owner/repo@{resolvedSha}`，作为实例来源标识与后续更新检查的依据。

## 非目标

- **不实现 packwiz 仓库的搜索与枚举。** 隐藏仓库不进列表，`SearchAsync` / `InspectAsync` / `Identify` / `ReadDescription` / `ReadChangelog` 全部抛 `NotSupportedException`（语义：本就不打算支持）。
- **不在 importer 内下载 mod JAR。** importer 只产出 pref 引用列表（`pref://modrinth/...` / `pref://curseforge/...`），实际下载、版本筛选、hash 校验、缓存全部复用现有 DataService → ModrinthRepository / CurseForgeRepository 链路。
- **不实现 packwiz 仓库的写入或反向导出。** 只消费标准格式。
- **不替代表现有的 zip 导入流程。** 两者并行。

## 关键决策

### 决策 1：仓库解析为整合包，mod 只产引用

`PackwizRepository` 的解析对象是**整个 modpack**（`Kind = Modpack`），不感知仓库内单个 mod。单个 mod 的解析由 `PackwizImporter` 扫描 `.pw.toml` 的 `[update]` 块后，把引用转交给现有仓库完成。

理由：mod 的版本/下载信息在 `.pw.toml` 的 `[update]` 块里已指向 Modrinth/CF，绕过它们在隐藏仓库内部重新聚合是冗余，且 packwiz 元数据不含聚合所需字段。

### 决策 2：vid 为 Git ref，解析为具体 commit SHA

- `pref://packwiz/owner/repo@v1.0.0` → tag
- `pref://packwiz/owner/repo@main` → 分支
- `pref://packwiz/owner/repo@abc1234` → commit
- 无 vid → 取默认分支最新 commit

Resolve/Query 时通过 GitHub Commits API（`GET /repos/{owner}/{repo}/commits/{ref}`）把任意 ref 解析为具体 commit SHA；`Package.Download` 始终用 `/archive/{sha}.zip`，指向不可变快照。记录进 `Setup.Source` 的也是这个解析后的 SHA，作为更新比对的基准。

### 决策 3：元数据经 Contents API 读取，不下载整个 archive

`QueryAsync` / `ResolveAsync` 只需 `pack.toml` 内容，通过 `GET /repos/{owner}/{repo}/contents/pack.toml?ref={ref}` 读取（返回 base64 内容），无需拉取整个 archive zip。仅在实例真正导入时才下载完整 archive 交给 importer。

### 决策 4：pack.toml 字段映射

`pack.toml` 只保证 `name`、`author`、`version`、`[versions]`。`Summary`、`Thumbnail`、`Tags`、`DownloadCount`、`Gallery` 等 `Project`/`Package` 必填字段以空值/默认值填充（`Summary = ""`、`Tags = []`、`DownloadCount = 0`、`Gallery = []`）。这些记录只挂在实例来源字段上用于更新判断，不在任何浏览/搜索 UI 展示，空字段无害。

`[versions]` 键到 Polymerium loader 身份（`LoaderHelper`）的映射：

| packwiz `[versions]` 键 | loader 身份 | lurl 示例 |
|---|---|---|
| `fabric` | `net.fabricmc` | `net.fabricmc:0.16.9` |
| `forge` | `net.minecraftforge` | `net.minecraftforge:47.3.0` |
| `neoforge` | `net.neoforged` | `net.neoforged:20.4.237` |
| `quilt` | `org.quiltmc` | `org.quiltmc:0.27.1` |

`[versions].minecraft` → `Setup.Version`。`pack.toml` 顶层 `name` → `Profile.Name`。

### 决策 5：`.pw.toml` 的 `[update]` 块映射为 pref

| `.pw.toml` 子表 | 产出 pref |
|---|---|
| `[update.modrinth]` | `pref://modrinth/{mod-id}@{version}` |
| `[update.curseforge]` | `pref://curseforge/{project-id}@{file-id}` |

- 一个 `.pw.toml` 正常只含一个 `[update]` 子表；若同时含两个，优先取 modrinth。
- 无 `[update]` 块（纯 `[download].url` 直链 mod）：跳过，不产出 pref——这类 mod Polymerium 无法追踪，由用户手动处理。
- `side` 为 `server` 的条目排除（与 `.mrpack` 对 env 的处理一致），`both` / `client` 纳入。
- CurseForge 的 `project-id` / `file-id` 在 TOML 中是整数，拼 pref 时需转字符串。

### 决策 6：非元数据文件按相对路径复制

`config/`、`scripts/`、`defaultconfigs/` 等非 `*.pw.toml` 文件，按其在仓库内的相对路径复制到实例 import 目录，与 `.mrpack` 的 `overrides/` 处理一致（剥去前缀后原样落盘）。不做 packwiz `index.toml` 的 hash 校验——archive 本身是自洽快照，无需复核。

## 决策 7：派发层用最长公共前缀兼容 wrapper 目录

GitHub codeload archive 永远把内容包进一层 `{repo}-{ref}/` 目录，下载下来的 zip 里是 `my-pack-abc123/pack.toml`，而非根级 `pack.toml`。而 `ImporterAgent` 现在按 `pack.FileNames.Contains(x.IndexFileName)` 字面匹配，根级 index 在带 wrapper 的 archive 里对不上，会识别失败。

不在下载侧重打包，也不让 importer 各自处理（派发失败时 importer 拿不到控制权）。改为在派发层统一兼容：

1. ImporterAgent 扫一遍 `pack.FileNames`，求所有条目的最长公共前缀字符串，再截到最近的 `/` 得到目录边界上的 `prefix`（flat archive 该值为空字符串）。
2. 派发匹配改为 `pack.FileNames.Contains(prefix + x.IndexFileName)`——prefix 为空时退化为现有根级匹配，对现有 4 个 importer 完全向后兼容。
3. `prefix` 由 ImporterAgent 在派发前算好，经 `CompressedProfilePack` 暴露给 importer（新增一个只读属性）。importer 构建 `ImportFileNames` 时 `Source` 仍用 zip 全名、`Target` 剥去 `prefix`——与 ModrinthImporter 剥 `overrides/` 同一套路。

这是对「单层 wrapper 归档」的通用兼容，不只服务于 packwiz。

## 影响面

| 领域 | 影响 |
|---|---|
| `IRepository` / `RepositoryAgent` | 新增 `IsHidden`；`Labels` 及派生 UI 过滤隐藏仓库 |
| 仓库抽象 | 新增 `PackwizRepository`（隐藏） |
| Pref 解析 | 复用现有 `pref://` 语法，无解析层改动；`packwiz` 只是一个新 label |
| 导入引擎 | 新增 `PackwizImporter`（`IndexFileName = "pack.toml"`）；`ImporterAgent` 派发改为按最长公共前缀匹配 index 文件，兼容单层 wrapper 归档 |
| 实例元数据 | `Setup.Source` 记录 packwiz pref |
| 外部服务（DataService） | 无改动——mod 引用走现有 resolve 链路 |
| 依赖 | 无新增——`Tomlyn 2.10.1` 已在项目内 |

## 验收

| # | 场景 | 期望 |
|---|---|---|
| 1 | Resolve `pref://packwiz/owner/repo@v1.0.0` | 经 Commits API 解析为 SHA；返回 `Kind = Modpack` 的 `Package`，`Download` 指向 `/archive/{sha}.zip` |
| 2 | Query 同一 pref | 经 Contents API 读 `pack.toml`；返回 `Project`，含 `name`/`author`/Minecraft 版本/loader |
| 3 | `SearchAsync` / `InspectAsync` / `Identify` 等 | 抛 `NotSupportedException` |
| 4 | 隐藏仓库可见性 | `Labels` 与搜索/选择器 UI 不出现 `packwiz` |
| 5 | `PackwizImporter` 处理 archive | 扫描 `mods/*.pw.toml` 等提取 `[update]` 块为 `Setup.Packages` 中的 pref；非元数据文件按相对路径复制到 import 目录 |
| 6 | mod 无 `[update]` 块 | 跳过，不产出 pref |
| 7 | `pack.toml` 仅含必填字段 | Resolve 正常，缺失字段填默认值 |
| 8 | 实例来源更新检查 | 经 GitHub API 比对 `Setup.Source` 记录的 SHA 与远端当前 ref 的 SHA；不一致提示有更新 |
