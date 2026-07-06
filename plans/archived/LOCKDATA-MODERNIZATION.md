# LockData 现代化：双对象迁移模型，从部署缓存升级为版本锁定权威源

> 制定日期：2026-06-29
> 修订日期：2026-07-06（推翻原"在现有架构打补丁"的设计，改为双对象迁移模型 + viability 消散 + rule 融合 + 固定线性 pipeline）
> 定位：基础重构任务，是 Recipe 系统的前置条件之一。把 `data.lock.json` 从"构建过程缓存"重做为"可迁移的版本锁定权威源"，对标 npm `package-lock.json` / cargo `Cargo.lock`。
> 当前状态：蓝本，✅ 已实施（2026-07-06）。
> Jira：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)
> 依赖：无（独立基础设施任务）。

---

## 0. 本任务在 Recipe 系统中的位置

> Recipe 系统总纲：[RECIPE-SYSTEM.md](RECIPE-SYSTEM.md)

Recipe 的核心承诺是"导入后这些 mod 的版本被固定，不会因后续部署漂移"。这个承诺没有 LockData 的现代化就无法兑现——只要一次 rule 微调或任意无关包变更触发全量重新解析，recipe 带来的所有 mod 全部漂到最新版，"锁定"形同虚设。

因此 LockData 必须先从"部署过程缓存"升级为"版本锁定权威源"，Recipe（POLY-120）才能在其上兑现锁定承诺。

---

## 1. 背景与动机

### 1.1 现状

`LockData` 定义于 `submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/LockData.cs:6-14`，持久化为每个实例目录下的 `data.lock.json`（`PathDef.cs:81`）。`FORMAT = 1`。

顶层字段：`Viability` / `MainClass` / `JavaMajorVersion` / `GameArguments` / `JavaArguments` / `Libraries` / `Parcels` / `AssetIndex`。

`ViabilityData`（`LockData.cs:45-56`）：`Format` / `Watermark` / `RulesHash` / `Key` / `Version` / `Loader` / `Packages`（**原始浮动 purl 字符串列表**）。

`Parcel`（`LockData.cs:38-46`）：`Label` / `Namespace` / `Pid` / `Vid` / `Path` / `Download` / `Hash`——**已携带解析后的固定版本与 hash**。

### 1.2 现状部署管线如何使用 LockData

`DeployEngine.DecideNext`（`Engines/DeployEngine.cs:69-131`）是 `Manifest / Artifact / ArtifactBuilder` 三态状态机：

```
CheckArtifactStage ──Verify() 通过──→ Context.Artifact → EnsureRuntime → GenerateManifest → Solidify
   │
   └──Verify() 失败/缺失──→ Context.ArtifactBuilder = new()
                                → InstallVanilla → ProcessLoader → ResolvePackage → BuildArtifact(写 lock)
```

- `CheckArtifactStage`（`Stages/CheckArtifactStage.cs:9-48`）调 `LockDataExtensions.Verify`（`LockDataExtensions.cs:8-32`），比对 `Format / Watermark / RulesHash / Key / Version / Loader`，以及 **`Packages` 集合用 `SetEquals` 比较 purl 字符串**。
- 通过 → `Context.Artifact` 被设置，跳过全部构建 stage，`GenerateManifestStage` 直接从 `artifact.Parcels` 读固定 `Vid`。
- 失败 → 全量重建：`ResolvePackageStage`（`Stages/ResolvePackageStage.cs:7-42`）对每个 enabled 的浮动 purl 调 `PackagePlanner`（`PackagePlanner.cs:15-39`）→ `RepositoryAgent.ResolveBatchAsync` → 仓库 API 取最新匹配版本。

`BuildArtifactStage`（`Stages/BuildArtifactStage.cs:8-43`）是**唯一**写 lock 的地方：组装 `LockDataBuilder` → `Build()` → 序列化写 `data.lock.json`。

`InstanceManager.DeployAndLaunch` 有 FastMode 快速路径（`InstanceManager.cs:109-128`）：读 lock → `Verify` → 直接 `Launch`，不进 pipeline。

### 1.3 三个根因缺陷

| # | 缺陷 | 后果 |
|---|------|------|
| 1 | **职责混杂** | `data.lock.json` 一个文件同时是"构建产物缓存"（vanilla/loader 的 libraries/assets/args）和"mod 解析结果"（Parcels）。前者是本地缓存，后者本该是可迁移的依赖锁。 |
| 2 | **Verify 容忍漂移** | `Verify` 只比 purl **字符串集合** SetEquals，**不比 vid、不比 hash**（`LockDataExtensions.cs:8-27`）。浮动 purl 每次解析出不同 vid，缓存照样命中——漂移被"容忍"而非"防止"。 |
| 3 | **全有或全无 + 不可迁移** | Verify 是一锤子门控：任何一项变更（改一条 rule 的 Destination）→ `RulesHash` 变 → 全量重解析 → 全体 mod 漂到最新版。且文件躺在 `~/.trident/instances/<key>/`，混了本地 `Key` 字段，无法放 git 跨机器复现。 |

**典型踩坑**：用户精心调好一整套 mod 版本，调整一个资源包的部署目录（改 rule）→ 下次部署所有 mod 静默升级，兼容性炸裂，用户毫不知情。

### 1.4 漂移的物理来源

- 浮动 purl（无 `@vid`）每次构建都重新问仓库 API 取最新匹配版本（`CurseForge`/`Modrinth` 按 `DatePublished` 排序取首个）；`RepositoryAgent` 还有 7 天滑动缓存（`RepositoryAgent.cs:25`），跨机器/过期后结果不同。
- `Verify` 不检查 vid，所以旧 `Parcels` 被当缓存命中复用——漂移被掩盖。

---

## 2. 目标 / 非目标 / 不做的事

**目标**

1. `data.lock.json` 成为**版本锁定权威源**：每个包一旦解析，固定 `Vid` + `Download` + `Hashes` 被锁定，后续部署除非用户主动更新否则不漂移、不碰仓库。
2. **可迁移**：lock 文件能放入 git、跨机器精确复现同一组 mod（同 vid + hash 校验下载）。
3. **增量构建**：保留"避免全量构建"的缓存价值，但缓存判定从"全有或全无"细化为"按段、按包"。
4. **架构整洁**：Profile 表达意图（浮动 purl），LockData 表达事实（锁定版本），职责分离。

**非目标（本次不做）**

- 不改 `Entry.Purl` 存浮动 purl 的现状——Profile 永远是意图层。
- 不回写锁定版本到 `profile.json`（附录 A.1）。
- 不改部署的符号链接/缓存目录布局。
- 不实现跨实例共享 lock（单实例单 lock）。
- 不实现"自动嗅探上游有无新版本"——更新是用户显式动作。

**不做的事**

- 不细分 artifact 段为 vanilla-derived / loader-derived——两者 args 强耦合（Forge 会 `ClearGameArguments` 重加、改 mainClass），整体随 platform 生灭。
- 不引入第二个 lock 文件——版本锁定与构建缓存合并在同一个 `data.lock.json`（附录 A.2）。

---

## 3. 核心设计

### 3.1 双对象迁移模型（BaseLock / Lock）

本设计的基石。每个部署周期内同时存在两个 `LockData` 实例：

| 对象 | 角色 | 生命周期 |
|------|------|----------|
| `BaseLock` | **只读参照**：磁盘上旧的 lock 全貌，是各 stage 判断"上游是否有效"的稳定源头 | LoadLockStage 读入，全程不变 |
| `Lock` | **本次产物**：全新 `new` 出来，各 stage 逐步迁移/重建字段进去 | LoadLockStage 创建，PersistLockStage 写回磁盘 |

**核心原则：判断看 BaseLock（稳定源头），修改写 Lock（产物）。两者解耦。**

这正面解决两个难题：
- **artifact 部分填充的判断死结**：stage 不再看"Lock.Artifact 是否空"（会被中间态迷惑），而是看 `BaseLock.Platform` 是否匹配——源头有效就迁移/跳过，源头失效就重建。
- **platform.Version 影响浮动包**：SyncPackages 自己看 `BaseLock.Platform.Version` 精确判断浮动 purl 失效，而不是粗暴的"统一清空"。

### 3.2 新 LockData 结构

```csharp
public record LockData
{
    public const int FORMAT = 2;   // 结构性变更，从 1 升版

    public required PlatformData Platform { get; init; }   // LoadLock 必填，非空
    public string? OptionsHash { get; init; }              // Hash(DeployOptions)，顶层生成元数据
    public ArtifactData? Artifact { get; init; }           // 平台计算缓存，可空（待迁移/重建）
    public IReadOnlyList<LockedPackage> Packages { get; init; } = [];
}

public record PlatformData(string Minecraft, string? Loader);   // 内联 record，值比较

public record ArtifactData(                                   // 平台计算缓存段，整体随 platform 生灭
    string MainClass,
    uint JavaMajorVersion,
    IReadOnlyList<string> GameArguments,
    IReadOnlyList<string> JavaArguments,
    IReadOnlyList<Library> Libraries,
    AssetData AssetIndex
);

public record LockedPackage(
    string Purl,                // 声明态 purl（= Entry.Purl，作 diff 键）
    string? Source,             // 归属（POLY-118 的 Manual/Modpack/Recipe/Legacy）
    ResolvedPackage Resolved,
    PackageRule Rule,           // rule 评估结果，融合进 package
    Compatibility? Compat
);

public record ResolvedPackage(
    string Vid,                 // 锁定的固定版本 ID
    string Label,               // rule 评估所需（RuleHelper.Repository 选择器）
    ResourceKind Kind,          // rule 评估所需（RuleHelper.Kind 选择器）
    string ProjectName,         // target 计算（Normalizing 时用）
    string FileName,            // target 计算
    Uri Url,
    long Size,
    FileHashes Hashes           // sha1 + sha512，下载校验 + 完整性
);

public record PackageRule(bool Skipping, string? Destination, bool Normalizing);

public record Compatibility(IReadOnlyList<string> GameVersions, IReadOnlyList<string> Loaders);

// Library / AssetData / FileHash / FileHashes 基本不变
```

**关键设计点：**

- `Parcels`（线性列表）+ `Viability.Packages`（purl 字符串列表）→ 合并为 `Packages`（每个 entry 既含声明 purl，又含完整 resolved + rule 结果）。
- `ResolvedPackage` 比 现 `Parcel` 多缓存 `Label / Kind / ProjectName / FileName`——供 rule 变化时用缓存重建 `RuleHelper.Input` 重算评估，**零网络**。
- `Platform` 是内联非空 record：LoadLock 必填，各 stage 用 `BaseLock.Platform == Lock.Platform`（record 值相等）比较，无需 null 判断、无需处理 Setup 原始字段。
- `Artifact` 可空：空 = 待迁移或重建。

### 3.3 viability 段消散

`ViabilityData` 作为一个段**整体消失**，字段重新分配：

| 原 Viability 字段 | 去向 |
|-------------------|------|
| `Format` | → 顶层 `FORMAT` 常量 |
| `Watermark` | → 顶层 `OptionsHash`（改名，反映真实计算对象 `Hash(DeployOptions)`）|
| `RulesHash` | **删除**——rule 评估结果融合进每个 `LockedPackage.Rule`，rule 变化只重算受影响包 |
| `Key` | **删除**——本地实例标识，阻碍可迁移性；文件按实例目录组织天然绑定 |
| `Version` / `Loader` | → `Platform` 段 |
| `Packages`（purl 列表）| **删除**——融合进 `Packages[].Purl` |

### 3.4 固定线性 pipeline（DecideNext 退化）

现状 `DecideNext` 的 `Artifact / ArtifactBuilder / Manifest` 三态分支（`DeployEngine.cs:69-131`）整体废弃。改为**固定线性序列**，`DecideNext` 退化为按列表 `yield`，不查状态分支：

```
LoadLockStage → InstallVanillaStage → ProcessLoaderStage → SyncPackagesStage
  → PersistLockStage → EnsureRuntimeStage → GenerateManifestStage → SolidifyManifestStage
```

每个 stage 内部用"看 BaseLock 判断有效性 + 迁移/重建到 Lock"自决干不干活，不需要被状态机跳过。单向天然成立。

**FastMode 快速路径保留在 `InstanceManager` 层**（`InstanceManager.cs:109-128`），在 pipeline **之外**：lock 全有效时读 lock + 判定 + 直接 `Launch`，不进 DeployEngine。这条路径保留，与固定序列不冲突。

### 3.5 各 stage 的迁移/重建逻辑（判断一律看 BaseLock）

| Stage | 看什么判断 | 动作 |
|-------|-----------|------|
| **LoadLockStage**（原 CheckArtifact） | — | 读文件 → `BaseLock`（不存在或旧 `FORMAT=1` 反序列化失败 → null）；`Lock = new LockData { Platform = from Setup, OptionsHash = Hash(Options) }`。**不做失效判断**，失效判断分散到各 stage |
| **InstallVanillaStage** | `BaseLock?.Platform == Lock.Platform` 且 `BaseLock?.Artifact != null`？ | 匹配 → **整体迁移** `BaseLock.Artifact` → `Lock.Artifact`（vanilla+loader 一起，原子）；不匹配 → 调 PrismLauncher 重建 vanilla 部分填入 `Lock.Artifact` |
| **ProcessLoaderStage** | `BaseLock?.Platform == Lock.Platform`？ | 匹配 → **skip**（artifact 已被 InstallVanilla 整体迁，loader 在里面）；不匹配 → 重建 loader 部分追加到 `Lock.Artifact` |
| **SyncPackagesStage**（原 ResolvePackage） | 见 §3.6 | diff + 精细 rule + 浮动包失效 |
| **PersistLockStage**（原 BuildArtifact 的保存部分） | — | 序列化 `Lock` → 写 `data.lock.json`（**唯一写回点**） |
| EnsureRuntimeStage | — | 查 Java（不变） |
| GenerateManifestStage | — | 从 `Lock` 生成下载清单（消费 `Lock.Artifact` + `Lock.Packages`） |
| SolidifyManifestStage | — | 下载/链接；**下载后用 `ResolvedPackage.Hashes` 校验**（现状只下载前判缓存，补上下载后校验闭环防 CDN 篡改） |

**artifact 整体随 platform**（§2 不做的事）：迁移是原子的（platform 匹配则整体迁），重建是分步的（platform 不匹配则 vanilla 先重建、loader 跟着重建）。ProcessLoader 只看 `BaseLock.Platform`——匹配就 skip（整体迁过了），不匹配就重建 loader，绝不被 Lock 中间态迷惑。

### 3.6 SyncPackagesStage：diff 视图 + 精细 rule + 浮动包失效

**第一步：建 diff 视图**（按 purl 匹配，**不考虑 vid**）

```
Setup.Packages(enabled) 与 BaseLock?.Packages 按 Purl 建三桶：
  Added   : Setup 有、BaseLock 无
  Removed : BaseLock 有、Setup 无（丢弃，不迁移）
  Matched : 双方都有（按 purl 匹配，vid 差异不在此步处理）
```

**第二步：对 Matched 桶逐包判定 resolved 有效性**

```
对每个 matched package：
  该 entry 的 purl 是浮动（无 @vid）还是固定（有 @vid）？
    浮动 + (BaseLock.Platform != Lock.Platform 或 BaseLock.OptionsHash != Lock.OptionsHash)
      → resolved 失效，进 resolve 集合（filter/策略变了）
    固定 → resolved 不失效（vid 固定），但需重算 rule（见下）
```

> **vid 模糊继承**：diff 第一步按 purl 不考虑 vid 匹配，所以一个 entry 从固定（`@vid`）变浮动（去掉 vid）仍能匹配上、继承原 resolved——只有当 platform/optionsHash 变化时才按浮动规则重新判定失效。

**第三步：精细 rule 失效**（所有 matched 包，含 resolved 有效的）

```
对每个 matched package：
  用 BaseLock 里缓存的 ResolvedPackage（重建 RuleHelper.Input）+ 当前 Setup.Rules 重跑 RuleHelper.Evaluate
  比对 BaseLock 里存的 PackageRule → 变了才更新该包的 Rule 字段
  （零网络、零重新 resolve）
```

**第四步：组装 Lock.Packages**

```
Matched 且 resolved 有效 → 迁移 BaseLock 的 Resolved + (重算后的) Rule 到 Lock.Packages
Matched 且 resolved 失效 → resolve(网络) + 算 rule → 填 Lock.Packages
Added → resolve(网络) + 算 rule → 填 Lock.Packages
Removed → 不迁移
```

**为什么精细 rule 失效是必须的（不是优化）**：rule 变化若导致全部重 resolve，会重新触发仓库 API 解析浮动 purl → **浮动包又漂了**，直接破坏版本锁定。所以 rule 变化必须只用缓存 resolved 重算评估，绝不重新 resolve。这与防漂移目标一致。

### 3.7 rule 融合进 package

`RulesHash`（全局）删除，每个 `LockedPackage` 自带 `Rule` 字段记录当时的 rule 评估结果（`Skipping / Destination / Normalizing`）。rule 变化时 SyncPackages 用缓存 resolved 重算，只更新受影响包的 `Rule` 字段，其余包和所有 resolved 原封不动。

`PackagePlanner.RecomputeRule(resolved, rules)` 是新增的纯函数：输入缓存的 `ResolvedPackage` + 当前 rules，产出新的 `PackageRule`。供 SyncPackages 精细失效用，不碰仓库。

### 3.8 命名重构

| 旧 | 新 | 备注 |
|----|----|------|
| `ViabilityData.Watermark` | 顶层 `LockData.OptionsHash` | 反映真实计算对象 `Hash(DeployOptions)`，与 `RulesHash`（已删）对称来源 |
| `Context.VerificationWatermark` | `Context.OptionsHash` | |
| `Context.Artifact`（类型 LockData） | `Context.Lock` | artifact 概念下放给内部段 |
| `Context.ArtifactBuilder` | **删除** | 双对象模型下不需要 builder；新增 `Context.BaseLock` |
| `CheckArtifactStage` | `LoadLockStage` | 不再 verify 跳过，改为加载 + 建 Lock |
| `BuildArtifactStage` | `PersistLockStage` | 只负责保存，不再组装 |
| `ResolvePackageStage` | `SyncPackagesStage` | 反映差量同步语义 |
| `DeployStage.CheckArtifact` / `.BuildArtifact` | `.LoadLock` / `.PersistLock` | 枚举成员 |
| `ArtifactUnavailableException` | `LockUnavailableException` | |
| `LockDataBuilder` / `LockDataBuilderExtensions` | **删除** | 直接操作 `LockData` 对象 |
| `LockData.Artifact`（新内部段） | 类型 `ArtifactData` | 保留 Artifact 词，语义收窄为"平台计算缓存段" |

**不动的 Artifact**：`AuthlibInjectorArtifact*`（authlib-injector API 术语）、`PrismLauncher Component.ArtifactEntry`（外部 API 模型）——别人的概念。

资源键 `DeployStage_CheckArtifact` / `DeployStage_BuildArtifact` → `DeployStage_LoadLock` / `DeployStage_PersistLock`，三份本地化文件（`Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs`）同步。

### 3.9 可迁移性

- 删除 `Viability.Key`（本地实例标识）。
- `Path` 字段（原 `Parcel.Path`）贯彻**相对 build 目录的相对路径**——跨机器一致。
- 整个文件去本地专属内容后纯可复现：`profile.json` + `data.lock.json` 一起进 git，另一台机器拿到即能精确复现同一组 mod（同 vid + hash 校验下载）+ 同一套 vanilla/loader 产物。

### 3.10 旧格式降级

`FORMAT` 从 1 升到 2，结构不兼容。LoadLockStage 反序列化时：旧 `FORMAT=1` 文件按新结构反序列化失败 → `BaseLock = null` → 全部字段待重建 → 等同首次部署。**数据无损**（profile 还在，重新 resolve）。这是有意为之的破坏性升级，不做双读迁移（lock 是可重建的派生数据，不值得迁移成本）。

---

## 4. 改动面

> 行号为重构前基准，实施后会变。

| ✅🔨✅ | 层 | 文件 | 改动 |
|-------|----|------|------|
| ✅ | 数据模型 | `TridentCore.Abstractions/FileModels/LockData.cs` | 顶层结构按 §3.2 重写；`ViabilityData` 删除；新增 `PlatformData / ArtifactData / LockedPackage / ResolvedPackage / PackageRule / Compatibility`；`FORMAT=2` |
| ✅ | 数据模型 | `TridentCore.Abstractions/Extensions/LockDataExtensions.cs` | `Verify` 重写为双对象判定（FastMode 路径用：`BaseLock.Platform/OptionsHash` 匹配 + packages purl 集合匹配）|
| ✅ | 部署上下文 | `TridentCore.Core/Engines/Deploying/DeployContext.cs` | `Artifact`→`Lock`、新增 `BaseLock`、删 `ArtifactBuilder`、`VerificationWatermark`→`OptionsHash` |
| ✅ | Builder | `TridentCore.Core/Engines/Deploying/LockDataBuilder.cs` + `LockDataBuilderExtensions.cs` | **删除**——各 stage 直接操作 `LockData` |
| ✅ | 状态机 | `TridentCore.Core/Engines/DeployEngine.cs:69-131` | `DecideNext` 退化为固定线性序列 yield，删三态分支 |
| ✅ | Stage | `Stages/CheckArtifactStage.cs` → `LoadLockStage.cs` | 重写：读 BaseLock + new Lock + 填 Platform/OptionsHash |
| ✅ | Stage | `Stages/InstallVanillaStage.cs:8-126` | 改为看 `BaseLock.Platform` 比对 `Lock.Platform`：匹配→整体迁 Artifact；不匹配→重建 vanilla（直接写 `Lock.Artifact`，不经 builder）|
| ✅ | Stage | `Stages/ProcessLoaderStage.cs:10-174` | 改为看 `BaseLock.Platform`：匹配→skip；不匹配→重建 loader 追加到 `Lock.Artifact` |
| ✅ | Stage | `Stages/ResolvePackageStage.cs` → `SyncPackagesStage.cs` | 重写为 §3.6 的 diff + 精细 rule + 浮动失效 |
| ✅ | Stage | `Stages/BuildArtifactStage.cs` → `PersistLockStage.cs` | 只保留序列化写文件部分，删组装逻辑 |
| ✅ | Stage | `Stages/GenerateManifestStage.cs:11-157` | 消费 `Lock.Artifact` + `Lock.Packages`（原 `artifact.Parcels`）生成 manifest |
| ✅ | Stage | `Stages/SolidifyManifestStage.cs:12-237` | 下载后补 hash 校验（用 `ResolvedPackage.Hashes`）|
| ✅ | 规则 | `TridentCore.Core/Engines/Deploying/PackagePlanner.cs` | 拆出 `RecomputeRule(resolved, rules)` 纯函数供 SyncPackages 用 |
| ✅ | 上层 | `TridentCore.Core/Services/InstanceManager.cs:109-128,122,251,256-292` | FastMode 路径改用新 `Verify`；watermark 计算点改 `OptionsHash`；stage 枚举引用改名 |
| ✅ | 异常 | `Exceptions/ArtifactUnavailableException.cs` | → `LockUnavailableException` |
| ✅ | 枚举 | `DeployStage.cs:5,9` | `CheckArtifact`→`LoadLock`、`BuildArtifact`→`PersistLock` |
| ✅ | 本地化 | `Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs:2154-2156,2178-2180` | 资源键同步改名 + 三文件一致 |
| ✅ | 宿主 | `src/Polymerium.Avalonia`（若有引用） | grep 确认无 Artifact/Watermark 旧名残留引用 |

---

## 5. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 首次部署（无 lock） | `BaseLock=null`，全包 resolve，写入 `Lock.Packages`，`PersistLock` 落盘 |
| 2 | 二次部署，无任何变更 | `BaseLock.Platform==Lock.Platform`、optionsHash 一致 → InstallVanilla 整体迁 artifact、ProcessLoader skip、SyncPackages 全部 matched 且有效 → **零网络**，仅本地 IO |
| 3 | 仅改一条 rule 的 `Destination` | 受影响包 `Rule` 字段重算（用缓存 resolved），`Resolved.Vid` 不漂移，**零 API 调用** |
| 4 | 新增一个 mod | 仅新 mod 进 Added 桶 resolve，其余锁定版本不动 |
| 5 | 删除一个 mod | 该 purl 进 Removed 桶，不迁移，`Lock.Packages` 不含它 |
| 6 | 改 Minecraft 版本（platform 变） | `BaseLock.Platform != Lock.Platform` → artifact 整体重建；浮动包 resolved 全失效重 resolve；固定包 resolved 保留、rule 重算 |
| 7 | 改 loader（platform 变） | 同 #6 |
| 8 | entry 从固定（`@vid`）改浮动（去 vid） | diff 按 purl 仍匹配（vid 模糊继承），继承原 resolved；若同时 platform 变则按浮动规则重 resolve |
| 9 | 调用单包更新 | 该包 resolved 失效重 resolve，其余不动（更新 API 由后续任务接 UI，本任务先保证机制）|
| 10 | `data.lock.json` 损坏或旧 `FORMAT=1` | LoadLock 反序列化失败 → `BaseLock=null` → 全量重建（降级，数据无损）|
| 11 | 跨机器：把 profile.json + data.lock.json 复制到新实例 | 精确复现同一组 mod（同 vid + hash 校验），无字段因本地 Key 失配 |
| 12 | 下载完成后 | Solidify 用 `ResolvedPackage.Hashes` 校验内容，不匹配报错（防 CDN 篡改）|

场景 2、3 是与现状对比的核心收益：现状会全量漂移，新机制零漂移零网络。

---

## 6. 风险与取舍

| 风险 | 取舍 |
|------|------|
| `FORMAT` 升版破坏旧 lock | 旧 lock 视为 null 全量重建；lock 是可重建派生数据，不值得双读迁移（附录 A.3）|
| 双对象增加一次内存占用 | BaseLock 只读、体积有限（KB 级），可忽略 |
| 固定序列每次跑全部 stage | 命中缓存的 stage 内部秒过（查字段），开销可忽略；放弃"完美命中跳过前半段"优化换状态机简洁，值 |
| rule 评估依赖 resolved 的 Label/Kind 等字段 | 这些在 vid 确定后稳定，已缓存进 `ResolvedPackage`；`RecomputeRule` 用缓存重算，零网络 |
| 锁定后仓库下架某版本，下载失败 | Solidify 报错提示用户；不自动漂移（lock 本意：可复现优先于新鲜）|
| FastMode 路径的 Verify 需重写 | 改为双对象语义：比对 platform/optionsHash + packages purl 集合；逻辑收敛在 `LockDataExtensions`，InstanceManager 调用点不变 |

---

## 7. 不做的事（明确边界）

- **不回写 `profile.json`** —— Profile 保持纯意图层，锁定事实只在 LockData（附录 A.1）。
- **不引入第二个 lock 文件** —— 版本锁定与构建缓存合并于 `data.lock.json`（附录 A.2）。
- **不细分 artifact 段** —— vanilla/loader 的 args 强耦合，整体随 platform 生灭。
- **不实现跨实例 lock 共享** —— 单实例单 lock。
- **不做旧格式迁移** —— FORMAT 升版即破坏，旧文件降级全量重建。
- **不在本任务实现"按组优先级决定锁定哪个版本"** —— 那是 POLY-117（DEPLOYMENT-PRIORITY-BY-GROUP）的范畴；本任务只保证"锁定后不漂移"。
- **不实现 recipe 导入** —— recipe 的 `Source = recipe://` 赋值归 POLY-120，本任务只保证 lock 能接纳。

---

## 附录：备选方案备案

### A.1 锁定版本存哪里

当前选：**LockData 内 `LockedPackage.Resolved`**（§3.2）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 给 `Entry` 加 `LockedVid` 回写 `profile.json` | 部署后写回 Entry | 破坏架构：Profile 既是意图又是事实；部署流程当前只读 Profile，回写需引入写锁链路。Profile 保持纯净 |
| 新建独立 `locks.json` 与 Profile 并列 | 单独文件 | 增加同步点；LockData 本就是部署产物，天然适合承载锁 |

### A.2 版本锁定与构建缓存：一个文件还是两个

当前选：**合并于 `data.lock.json`**（双职责）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 拆 `profile.lock.json`（版本锁定）+ `data.lock.json`（构建缓存） | 分两层 | 调研确认 `data.lock.json` 几乎无本地专属内容（仅 `Key` 删之即净）；且版本锁定失效必导致构建缓存失效，两者联动，分文件徒增同步。合并更紧凑 |

### A.3 旧 FORMAT 迁移

当前选：**不迁移，降级全量重建**

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 双读迁移（旧格式读入转新结构） | 反序列化旧 LockData + 字段映射 | lock 是可重建的派生数据（profile 是源头），迁移成本 > 价值；直接重建数据无损 |

### A.4 状态机：固定序列 vs 分支跳转

当前选：**固定线性序列，DecideNext 退化**（§3.4）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 保留 DecideNext 分支（缓存完美命中跳过前半段）| 状态机驱动 | 分支的存在正是为"跳过 stage"，而双对象模型下每个 stage 内部自决已足够；分支带来"Lock 中间态迷惑"问题（artifact 部分填充判断死结）。线性序列更简单、单向天然成立 |

### A.5 rule 失效粒度

当前选：**精细 per-package，用缓存 resolved 重算**（§3.6/§3.7）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 全局 RulesHash 变 → 全包失效 | 现状模式 | 导致全包重 resolve → 浮动包漂移，**与防漂移目标直接冲突**。不是优化取舍，是必须精细 |

### A.6 判断依据：Lock 中间态 vs BaseLock 源头

当前选：**看 BaseLock**（§3.1）

| 备选 | 做法 | 否决理由 |
|------|------|----------|
| 看 Lock 字段是否空决定 stage 干不干 | "字段非空自决" | InstallVanilla 填了 vanilla 部分后 Lock.Artifact 非空，ProcessLoader 分不清"整体迁好了"还是"只重建一半"。判断必须依赖稳定的 BaseLock 源头，不是被修改的 Lock 中间态 |
