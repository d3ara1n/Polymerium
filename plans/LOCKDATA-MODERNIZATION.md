# LockData 现代化：从部署缓存升级为版本锁定权威源

> 制定日期：2026-06-29
> 定位：基础重构任务，是 Recipe 系统的前置条件之一。
> 当前状态：**未实施**。本文档是未来实施的蓝本，据此编码不需重新调研。
> Jira：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)
> 依赖：无。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Polymerium 计划引入 Recipe 系统（用户可复用、无副作用的 mod 预制清单，区别于带副作用的整合包）。Recipe 的核心承诺是"导入后这些 mod 的版本被固定，不会因后续部署漂移"。这个承诺没有 LockData 的现代化就无法兑现——只要一次 rule 微调或任意无关包变更，就会触发全量重新解析，recipe 带来的所有 mod 全部漂到最新版，"锁定"形同虚设。

因此 LockData 必须先从"部署过程缓存"升级为"真正的版本锁定权威源"，对标 npm `package-lock.json` / cargo `Cargo.lock` 的语义。本文档即此重构的设计蓝本。

---

## 1. 背景与动机

### 1.1 现状

LockData 定义于 `submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/LockData.cs:6-14`，持久化为每个实例目录下的 `data.lock.json`，由 `BuildArtifactStage`（`Engines/Deploying/Stages/BuildArtifactStage.cs:9-36`）在部署末尾写入。

顶层字段：

| 字段 | 类型 | 含义 |
|------|------|------|
| `Viability` | `ViabilityData` | 验证元数据（决定本 LockData 能否复用） |
| `MainClass` / `JavaMajorVersion` | string / uint | Java 主类与所需大版本 |
| `GameArguments` / `JavaArguments` | `IReadOnlyList<string>` | 游戏参数与 JVM 参数 |
| `Libraries` | `IReadOnlyList<Library>` | MC 库与原生库 |
| `Parcels` | `IReadOnlyList<Parcel>` | **已解析的包列表**（含固定 `Vid`） |
| `AssetIndex` | `AssetData` | 资源索引 |

`Parcel`（`LockData.cs:52-59`）已携带解析后的固定版本：`Label`、`Namespace`、`Pid`、**`Vid`**、`Path`、`Download`、`Hash`。

`ViabilityData`（`LockData.cs:69-77`）：`Format`、`Watermark`、`RulesHash`、`Key`、`Version`、`Loader`、`Packages`（**原始 profile purl 字符串列表**——浮动 purl）。

### 1.2 当前部署管线如何使用 LockData

`DeployEngine.DecideNext`（`Engines/DeployEngine.cs:63-97`）的 8 阶段状态机：

```
CheckArtifactStage ──Verify() 通过──→ (FastMode 可直接启动)
   │                                   否则: EnsureRuntime → GenerateManifest → SolidifyManifest
   └──Verify() 失败/缺失──→ InstallVanilla → ProcessLoader → ResolvePackage → BuildArtifact(写 LockData)
```

- **`CheckArtifactStage`**（`Stages/CheckArtifactStage.cs:13-41`）调用 `LockDataExtensions.Verify`（`TridentCore.Abstractions/Extensions/LockDataExtensions.cs:8-28`），比对 `Viability` 与当前状态：Format、Watermark、RulesHash、Key、Version、Loader，以及 **`Packages` 集合用 `SetEquals` 比较 purl 字符串**。
- 通过 → `Context.Artifact` 被设置，跳过构建阶段，`GenerateManifestStage` 直接从 `artifact.Parcels` 读固定 `Vid` 生成 manifest。
- 失败 → 全量重建：`ResolvePackageStage`（`Stages/ResolvePackageStage.cs`）对每个 enabled 的浮动 purl 调 `PackagePlanner` → `RepositoryAgent.ResolveBatchAsync` → 仓库 API **取最新匹配版本**（`CurseForgeRepository` / `ModrinthRepository` 在 `vid == null` 时按 filter 取 `DatePublished` 最新）。

### 1.3 问题：LockData 退化为"部署缓存"，而非"版本锁定"

| 缺陷 | 后果 |
|------|------|
| **`ViabilityData.Packages` 存浮动 purl，`Verify` 用 `SetEquals` 整集比较** | 任何一项变更 → 整集不等 → Verify 失败 → 全量重建 |
| **Verify 是全有或全无的门控** | 用户只是改了一条 rule 的 `Destination`（部署路径），`RulesHash` 变 → 所有包重新解析到最新版 → 全体版本漂移 |
| **没有 per-package 锁定语义** | 无法"锁定某个 mod 不动，其余正常更新"；无法只重解析新增包 |
| **没有"手动更新单个包"的 API** | 想"更新 JEI 到最新"只能触发某种导致 Verify 失败的操作，副作用不可控 |

**典型踩坑**：用户精心调好了一整套 mod 版本，然后调整了一个资源包的部署目录（改 rule），下一次部署 → 所有 mod 静默升级到最新版，兼容性炸裂，且用户毫不知情。

### 1.4 为什么现在必须做

Recipe 系统要求"导入即锁定"。若不解决上述漂移问题，recipe 承诺的"版本固定"在任何一次无关部署变更后都会失效。LockData 现代化是 Recipe 落地的物理基础。

---

## 2. 目标与非目标

**目标**

1. LockData 成为**版本锁定的权威源**：每个包一旦解析，其固定 `Vid` 被锁定，后续部署除非用户主动更新否则不漂移。
2. **per-package 锁定与校验**：增量更新——只重解析"新增 / 用户要求更新 / 锁失效"的包，保留其余锁定版本。
3. 提供**显式的"更新"语义**：用户可主动更新单包或全量更新，更新结果回写进 LockData。
4. 保持**架构整洁**：Profile 表达意图（浮动 purl），LockData 表达事实（锁定版本），职责清晰分离，不互相回写污染。

**非目标（本次不做）**

- 不改变 `Entry.Purl` 存浮动 purl 的现状——Profile 永远是意图层。
- 不回写锁定版本到 `profile.json`（见 §7 备选 A.1，该方案已否决）。
- 不改变部署的符号链接/缓存目录布局。
- 不实现跨实例共享 lock（单实例单 lock）。

---

## 3. 核心设计：意图/事实分离

### 3.1 职责重新划分

| 层 | 职责 | 内容 |
|----|------|------|
| **`Profile.Rice.Entry`（意图）** | 用户想要什么 | 浮动 purl（`curseforge:jei#loader=fabric`）+ Enabled + Source + Tags。**不含任何已解析版本** |
| **`LockData`（事实）** | 部署实际锁定了什么 | 每个 Entry 对应一个 Parcel，含固定 `Vid`、`Download`、`Hash`、`Path` |

这是 npm/cargo 的标准模型：`package.json` 写意图（`^1.2.0`），`package-lock.json` 写事实（`1.2.7`）。Polymerium 当前已有这个雏形（Profile 浮动、LockData 固定），缺的只是"LockData 是权威、不被轻易推翻"的语义。

### 3.2 per-package 锁定键

锁定的核心是建立 **Entry（浮动 purl）→ Parcel（固定 vid）** 的稳定映射。映射键用 Entry 的浮动 purl 本身（去 filter 的 project identity 维度）：

```
锁定键 = PackageHelper.ExtractProjectIdentityIfValid(entry.Purl)
        // 即 label:ns/pid，剥离 @vid 和 #filter
```

`PackageHelper.ExtractProjectIdentityIfValid` 已存在（`TridentCore.Abstractions/Utilities/PackageHelper.cs`），正好产出 `label:ns/pid` 形式，天然适合做锁定键。

`LockData` 维护一个 **`LockedPackages: IDictionary<string, Parcel>`**，以锁定键索引。

### 3.3 per-package Verify

`Verify` 从"整集 SetEquals"改为"逐包判定 + 收集变更集"：

```
对当前每个 enabled Entry:
    key = ExtractProjectIdentityIfValid(entry.Purl)
    if LockedPackages.TryGetValue(key, out parcel):
        # 已锁定：校验锁是否仍有效
        if 锁定时的 entry.Purl 与当前 entry.Purl 不变（filter/vid 意图未变）:
            复用 parcel（锁定生效，不漂移）
        else:
            标记为"需重解析"（用户的意图变了，如改了 loader filter）
    else:
        标记为"新增"（需解析并写入锁）
```

`Viability` 仍保留 `Format`/`Watermark`/`Key`/`Version`/`Loader`/`RulesHash` 作全局门控（这些变了意味着环境结构变了，需重建 vanilla/loader 部分），但 **`Packages` 集合比较降级为辅助信息**，不再决定 per-package 命运。

### 3.4 增量解析

`ResolvePackageStage` 改造为消费 Verify 产出的**变更集**，而非全量重解析：

```
变更集 = { 新增包, 意图变更包 }   # 仅这些需要调仓库 API
对变更集中每个包: 解析 → 产出 Parcel → 写入 LockedPackages（覆盖旧锁）
对其余已锁定包: 直接从 LockedPackages 取 Parcel（零 API 调用）
```

收益：改一条 rule 不再触发任何包重解析——`RulesHash` 变只影响部署路径层（`PackagePlanner` 的 `Destination` 重新计算 `Path`），`Parcel.Vid/Download/Hash` 原样保留。

### 3.5 RulesHash 变化的精确处理

这是关键细化点：rule 变更分两类——

| Rule 变更类型 | 影响 | 处理 |
|---------------|------|------|
| 只改 `Destination` / `Normalizing`（部署路径） | 影响 `Parcel.Path`，**不影响版本** | 重新计算受影响包的 `Path`，保留 `Vid/Download/Hash` |
| 改了 `Skipping`（跳过放置） | 影响是否进入 manifest | 调整 manifest 生成，不动锁 |

因此 `Parcel.Path` 不应进入"锁定"语义——它是派生量，可由 rule 重算。锁定的只有 `Vid/Download/Hash`。

### 3.6 显式更新语义

新增 ProfileManager 层 API：

```csharp
// 更新单个包：解除其锁，下次部署重解析到最新
Task UpdatePackageAsync(string key, string lockedPurlKey);

// 全量更新：解除所有包的锁
Task UpdateAllPackagesAsync(string key);
```

"解除锁"= 从 `LockedPackages` 移除该键 → 下次部署进入变更集 → 重新解析 → 写回新锁。语义清晰：**锁是默认状态，"更新"是显式动作**。

---

## 4. 数据结构变更

### 4.1 LockData 调整

```csharp
public record LockData
{
    // 全局门控（保留）
    public required ViabilityData Viability { get; init; }
    public string? MainClass { get; init; }
    public uint JavaMajorVersion { get; init; }
    public IReadOnlyList<string> GameArguments { get; init; }
    public IReadOnlyList<string> JavaArguments { get; init; }
    public IReadOnlyList<Library> Libraries { get; init; }
    public AssetData? AssetIndex { get; init; }

    // 包锁定（重构核心）
    public required IReadOnlyDictionary<string, Parcel> LockedPackages { get; init; }

    // 兼容：旧版的线性 Parcels 由 LockedPackages.Values 派生，不再独立存储
}

public record Parcel
{
    public required string LockedByKey { get; init; }   // 锁定键 label:ns/pid
    public required string SourcePurl { get; init; }     // 锁定时刻的 entry.Purl（意图快照，用于检测意图变更）
    public required string Label { get; init; }
    public string? Namespace { get; init; }
    public required string Pid { get; init; }
    public required string Vid { get; init; }            // 锁定的固定版本（不可漂移）
    public string? Path { get; set; }                    // 派生：由 rule 计算，可重算
    public string? Download { get; init; }
    public string? Hash { get; init; }
}
```

要点：
- `LockedPackages` 以 `LockedByKey`（project identity）索引，确保同 project 多版本意图下键唯一。
- `SourcePurl` 记录锁定时的意图，用于 §3.3 判定"用户是否改了意图"。
- `Path` 是 `set`——rule 变更时重算，不视为锁定的一部分。

### 4.2 ViabilityData 收窄

`ViabilityData.Packages`（浮动 purl 集合）**移除**——per-package 校验改由 `LockedPackages` 的 `SourcePurl` 与当前 `entry.Purl` 比对承担。`Viability` 只保留全局环境门控：`Format`、`Watermark`、`Key`、`Version`、`Loader`、`RulesHash`。

---

## 5. 改动面

| 层 | 文件 | 改动 |
|----|------|------|
| 数据模型 | `LockData.cs` | 顶层结构按 §4.1 调整；`ViabilityData` 移除 `Packages` |
| 构建 | `LockDataBuilder.cs` | `AddParcel` 改为 `SetLockedParcel(key, parcel)`（字典写，天然去重） |
| 校验 | `LockDataExtensions.cs` | `Verify` 改为产出 `VerifyResult { GlobalValid, ChangedKeys, AddedKeys, PathInvalidations }` |
| 部署 | `CheckArtifactStage.cs` | 把 `VerifyResult` 写入 Context 供后续 stage 消费 |
| 解析 | `ResolvePackageStage.cs` | 只对 `ChangedKeys ∪ AddedKeys` 调仓库 API；其余从 `LockedPackages` 取 |
| 路径 | `PackagePlanner.cs` | 拆出独立的 `RecomputePath(parcel, rules)`，供 rule-only 变更时重算 `Path` 而不动版本 |
| 写入 | `BuildArtifactStage.cs` | 把解析结果合并进 `LockedPackages`（保留未变更包的旧锁），序列化 |
| 上层 API | `ProfileManager.cs` | 新增 `UpdatePackageAsync` / `UpdateAllPackagesAsync`（解除锁） |
| UI | `InstanceSetupPageModel.cs` | 暴露"更新此包"/"更新全部"命令；显示"已锁定到 vX"标记 |

---

## 6. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 首次部署 | 全包解析，写入 `LockedPackages`，后续复用 |
| 2 | 仅改一条 rule 的 `Destination` | 受影响包 `Path` 重算，`Vid` 不漂移，零 API 调用 |
| 3 | 新增一个 mod | 仅新 mod 解析，其余锁定版本不动 |
| 4 | 禁用再启用某 mod | 锁仍在，复用原版本（不漂移） |
| 5 | 改某 mod 的 loader filter | 该 mod 进入变更集重解析，其余不动 |
| 6 | 调用 `UpdatePackageAsync(jei)` | JEI 锁解除，下次部署解析到最新并重新锁定 |
| 7 | `data.lock.json` 损坏/缺失 | 当作未锁定，全量解析重建（降级行为不变） |
| 8 | 删除某 mod | 对应键从 `LockedPackages` 移除 |
| 9 | 同 project 两个 Entry（见 DEPLOYMENT-PRIORITY-BY-GROUP） | 按 project identity 键去重，由优先级机制决定哪个版本胜出并锁定 |

场景 2、3 是与现状对比的核心收益：现状会全量漂移，新机制零漂移。

---

## 7. 风险与取舍

| 风险 | 取舍 |
|------|------|
| `LockedPackages` 用字典索引，旧版线性 `Parcels` 的消费方需改 | 消费方有限（`GenerateManifestStage`），改为遍历 `LockedPackages.Values`；改动可控 |
| 锁定后仓库下架了某版本，`Download` 失效 | 部署时下载失败 → 提示用户该锁失效 → 可手动 `UpdatePackageAsync` 重解析；不在锁机制内自动漂移 |
| 用户长期不更新，锁定的版本有安全/兼容问题 | 这是 lock 文件的本意（可复现优先于新鲜）；更新是显式动作，符合用户预期 |
| 升级时旧 `data.lock.json` 格式不兼容 | 首次加载旧格式 → 视为未锁定 → 全量解析重建（与场景 7 一致），数据无损 |

---

## 8. 不做的事（明确边界）

- **不回写 `profile.json`** —— Profile 保持纯意图层，锁定事实只在 LockData（见备选 A.1 否决理由）。
- **不锁定 `Parcel.Path`** —— Path 是 rule 派生量，rule 变更必须能重算，否则 rule 功能失效。
- **不实现跨实例 lock 共享** —— 单实例单 lock。
- **不自动检测上游版本是否更新** —— lock 文件不主动嗅探，更新是用户触发的显式动作。
- **不在此任务实现"按组优先级决定锁定哪个版本"** —— 那是 DEPLOYMENT-PRIORITY-BY-GROUP 的范畴，本任务只保证"锁定后不漂移"。

---

## 9. 触发实施的条件

- Recipe 系统正式启动开发前必须完成（recipe 的锁定承诺依赖本任务）。
- 或用户反馈"我的 mod 版本莫名其妙变了"类问题达到一定频次。

---

## 附录：备选方案备案

### A.1 锁定版本存哪里

当前选：**LockData 内 `LockedPackages`**（§4.1）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| **给 `Entry` 加 `LockedVid` 字段，回写 `profile.json`** | 部署成功后把 `Vid` 写回 Entry | **用户已否决**。破坏架构：①部署流程当前只读 Profile，回写需引入写锁链路；②Profile 既是意图又是事实，职责混淆；③小范围破坏不如整体重构。本设计选择 LockData 作权威源，Profile 保持纯净 |
| **新建独立 `locks.json` 与 Profile 并列** | 单独文件存锁定 | 增加一个文件与同步点，收益低于直接复用 LockData；LockData 本就是部署产物，天然适合承载锁 |

### A.2 锁定键的选择

当前选：**`ExtractProjectIdentityIfValid` 产出的 `label:ns/pid`**

| 备选 | 做法 | 取舍 |
|------|------|------|
| 用完整浮动 purl（含 filter）作键 | `curseforge:jei#loader=fabric` 直接做键 | 用户改 filter 意图时会被当作"新包"产生孤立锁，需额外清理；剥离 filter 的 project identity 更稳定 |
| 用自增整数 id | Entry 加 id | 需改 Entry 结构，且 id 在导入/导出时不稳定；purl 派生键天然跨导入导出稳定 |

### A.3 Verify 失败时的粒度

当前选：**per-package 增量**（§3.3）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 维持全有或全无 | 任何变更全量重解析 | 即现状，版本漂移问题不解决，本任务无意义 |
| 按 source 分组批量失效 | 某 source 的包一起重解析 | 粒度太粗，同 source 内个别包意图变更会牵连整组；per-package 更精确 |

### A.4 更新触发方式

当前选：**显式 API（`UpdatePackageAsync`）**（§3.6）

| 备选 | 做法 | 取舍 |
|------|------|------|
| 按时间 TTL 自动失效锁 | 锁超过 N 天自动重解析 | 违反 lock 可复现本意；用户无感知的版本变更正是要消除的痛点 |
| 提供"更新全部"按钮但不提供单包更新 | 全或无 | 粒度太粗，用户想更新一个 mod 会被迫接受全部更新；单包更新成本低，应提供 |
