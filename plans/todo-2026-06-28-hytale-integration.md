# Hytale 接入计划：多游戏架构改造（方案 B）

> 制定日期：2026-06-28
> 定位：长期战略计划。引入「游戏类型」一等公民概念，将 Polymerium 从单游戏（Minecraft）启动器升级为多游戏启动器，首个新增游戏为 **Hytale**（Hypixel Studios，2026.01.13 进入 Early Access）。
> **里程碑：Hytale 接入为 Polymerium 的下一个开发里程碑。**
> 当前状态：**调研完成，待评审**。本文档是后续实施的蓝本，据此编码不需重新调研。所有结论均经源码核查与外部文档交叉验证。

---

## 1. 背景与动机

### 1.1 为什么选 Hytale 作为第二个游戏

- **生态已就绪且活跃**：Hytale 已正式 Early Access，截至 2026.05 已更新到 Update 5；存在 6 个以上活跃第三方启动器（HyTaLauncher / Butter / HRS / HyCore / Battly4Hytale / Hytale-F2P），其中 **HyTaLauncher 是 C#/.NET 8 MIT 项目**，OAuth 流程与启动实现可直接移植，省去最耗时的逆向探索。
- **官方不提供的空白点 = 差异化机会**：官方启动器仅 Flatpak + Windows 安装包，**无 macOS 支持**；官方启动器 4 个 channel 各自全量副本，无共享层。Polymerium 是跨平台 Avalonia 桌面应用（macOS 天然覆盖），且现有的 persist/symlink 机制天然能做共享层——这是比官方启动器更省空间的设计。
- **模组生态可对接**：CurseForge 已开 Hytale 专区（gameId=70216），API 与 Minecraft 版完全相同，仅 gameId 不同；Modtale 开源（AGPLv3）REST API。Polymerium 已有 CurseForge 对接，复用成本极低。
- **模组模型反而更简单**：Hytale 模组装在服务端，客户端零安装，无 Forge/Fabric 这类加载器复杂度，`ProcessLoaderStage` 在 Hytale 场景下整个可跳过。

### 1.2 为什么现在做

第一个非 Minecraft 游戏是最好的重构时机——只有一套游戏在跑时，抽象什么都是预测；两套真实需求摆在一起，接口边界才能切准。等第三个游戏来再回退成多游戏架构，代价会高得多。

---

## 2. 关键决策：采用方案 B（共享骨架 + 多态实现）

### 2.1 评估过的两个方案

| 方案 | 描述 | 判定 |
|------|------|------|
| **扩展原有管线** | 把 `InstallVanillaStage`/`Igniter`/`LockData` 改成内部 `if (gameKind == Hytale) ... else ...` | ❌ 否决。单类塞满游戏分支违反开闭原则；每加一游戏核心类都要动，回归风险随游戏数指数增长；`LockData` 这个干净 JVM 产物模型会被污染成大杂烩 |
| **方案 B：独立游戏实现** | 物理形态不同的层抽象成接口，每游戏一套实现；其余层共享 | ✅ 采用 |

### 2.2 决策依据：逐层判断「这层面对的是游戏产物的物理形态，还是实例怎么管、文件怎么放」

| 架构层 | 面对什么 | 判定 | 理由 |
|--------|----------|------|------|
| 实例目录结构 / `PathDef` | 通用容器 | **共享** | 已完全通用 |
| `Profile` 数据模型 | 通用容器（Version/Loader 是字符串） | **共享**（加 `GameKind` 字段） | 字段语义本就是字符串 |
| persist / import / live / snapshot 目录机制 | 通用文件放置 | **共享** | 见 §4，四个机制经核查全部可复用 |
| 实例生命周期（`InstanceManager`/`ProfileManager`） | 通用协调器 | **共享** | 不感知游戏 |
| 模组来源（`IRepository` + CurseForge/Modrinth） | 框架通用 | **共享框架，多态实现** | 加 `HytaleCurseForgeRepository`（换 gameId） |
| modpack 导入（`IProfileImporter` + `ImporterAgent`） | 框架通用 | **共享框架，多态实现** | 加 Hytale importer |
| 账号系统（`IAccount` + `AccountConfigurer`） | 框架通用 | **共享框架，多态实现** | 加 Hytale OAuth Device Flow |
| **部署产物模型（`LockData`）** | **游戏产物的物理形态** | **必须多态** | Hytale 是原生 exe + 内嵌 JRE，不是 JVM mainClass + libraries + assets，根本不是一类东西 |
| **部署 stage（Install/ProcessLoader/EnsureRuntime/GenerateManifest）** | **游戏怎么装** | **必须多态** | 每个 stage 的 Minecraft 逻辑对 Hytale 无意义 |
| **启动引擎（`Igniter`/`LaunchEngine`）** | **游戏怎么跑** | **必须多态** | `java -jar` vs 启动原生 exe，是两种进程模型 |
| **运行时来源** | **JRE 哪来** | **必须多态** | Mojang Piston vs `launcher.hytale.com/jre.json` |

**核心洞察**：必须多态的四层（产物/stage/启动/运行时）无论叫 B 还是叫扩展，**都必须按游戏分实现**，因为 Hytale 和 Minecraft 的产物在物理上不是一类东西。这不是风格选择，是物理事实。

### 2.3 B 方案的两个常见误解（澄清）

- **「B = 完全独立的管线」** ❌ 不是。B 只在物理形态不同的层做隔离，其余 7 层（目录/模型/生命周期/persist/import/snapshot/模组来源/modpack 框架/账号框架）全部共享。这正是 Polymerium 现有架构该保留的优势。
- **「扩展原有管线更省事」** ❌ 是陷阱。第一个游戏也许省事，第三个游戏来时核心类已被分支占满，再加一层 if 不可维护；B 方案接入成本恒定，扩展方案接入成本随游戏数线性甚至超线性上升。

---

## 3. Hytale 技术事实（调研沉淀）

> 本节是后续编码的事实蓝本，所有结论有外部来源或源码核查支撑。

### 3.1 文件布局

```
{Hytale 根}/
├── install/{channel}/package/          # channel ∈ {release, beta, alpha, dev}
│   ├── game/latest/Client/             # HytaleClient 原生 exe + .dll/.so/.dylib
│   │   ├── HytaleClient(.exe)          # 原生启动桩二进制
│   │   ├── HytaleClient.dll            # NativeAOT 编译的 C# 游戏逻辑
│   │   └── *.dll / *.so / *.dylib
│   ├── game/latest/Server/             # HytaleServer.jar + 服务端资源
│   ├── game/latest/Assets.zip
│   ├── game/build-{BuildID}/           # demotion 保留的旧版本
│   ├── jre/latest/                     # Temurin 25.0.1+8-LTS（独立目录）
│   ├── launcher/{version}/             # 启动器自身各版本
│   └── sig/                            # 签名文件（启动器手动校验用）
└── UserData/                           # ★ 用户数据根目录（与 install/ 平级独立）
    ├── Saves/                          # 单人存档
    ├── Mods/                           # 客户端 Mod（.jar/.zip）
    ├── settings/                       # 用户设置
    └── logs/                           # 客户端日志
```

各平台根目录：Linux `$XDG_DATA_HOME/hytale`、Windows `%APPDATA%\Hytale`、macOS `~/Library/Application Support/Hytale`。

**来源**：gist.github.com/karol-broda/3491df132865ffb3a3cddcde1848fdd9、decomp-project/hytale-launcher `internal/hytale/knownpath.go`、low.ms/en-gb/knowledgebase/hytale-save-file-location

### 3.2 启动机制（与 Minecraft 根本不同）

```
HytaleClient(.exe) \
    --app-dir  "{gameDir}" \          # 游戏安装目录，可任意路径
    --java-exec "{jrePath}" \          # JRE 路径，可任意位置
    --user-dir "{userDataDir}" \        # ★ 用户数据目录，与 app-dir 完全独立
    --auth-mode {offline|...} \
    --uuid {uuid} \
    --name {playerName}
```

**关键事实**（两个独立社区启动器 HyTaLauncher C# + HRS Launcher Rust 双重验证）：
- `--app-dir` 与 `--user-dir` 是两个独立 CLI 参数，可指向完全不同的路径
- UserData 与游戏安装目录是平级独立目录，删游戏重装不会触及 UserData
- 动态库通过 `LD_LIBRARY_PATH`（Linux）加载，可被环境变量覆盖，不强制同分区
- **HytaleClient 没有启动时签名/完整性校验**——官方启动器 `sig/` 验证是「手动修复」功能；F2P 社区能对 `HytaleClient.dll` 做二进制补丁后正常启动，间接证明无运行时校验

### 3.3 OAuth Device Flow 认证

```
1. POST https://oauth.accounts.hytale.com/oauth2/device/auth
     client_id=hytale-downloader&scope=offline auth:downloader
   → 返回 device_code + verification_uri_complete
2. 用户浏览器授权
3. 轮询 POST https://oauth.accounts.hytale.com/oauth2/token
     client_id=hytale-downloader&grant_type=urn:ietf:params:oauth:grant-type:device_code
     &device_code={device_code}
   → access_token + refresh_token
4. token 保存为 account.dat（AES-GCM 加密）
```

### 3.4 版本清单 API

| 端点 | 用途 | 认证 |
|------|------|------|
| `GET https://account-data.hytale.com/game-assets/version/{patchline}.json` | 获取最新版本 | 需 access_token |
| `GET https://account-data.hytale.com/game-assets/{download_url}` | 获取预签名下载 URL | 需 access_token |
| `GET https://launcher.hytale.com/version/{patchline}/launcher.json` | 启动器更新清单 | 公开 |
| `GET https://launcher.hytale.com/version/{patchline}/jre.json` | JRE 清单 | 公开 |

**版本号格式**：`YYYY.MM.DD-{commit_hash}`（如 `2026.01.24-6e2d4fc36`）。**关键限制：官方无版本列表端点**，只能拿最新版本，历史版本靠社区 HytaleDB 维护。

**响应示例**：
```json
{
  "version": "2026.01.24-6e2d4fc36",
  "download_url": "builds/release/2026.01.24-6e2d4fc36.zip",
  "sha256": "77e8b08465819dc46a03af1377126c3202fae3cd11bbd11afd9b8b2386436b16"
}
```

### 3.5 增量更新（可选，非 MVP）

Hytale 使用 itch.io 的 wharf 库做 `.pwr` delta 补丁，用 `butler` 工具应用：
```
https://game-patches.hytale.com/patches/{platform}/{arch}/{branch}/{prevVersion}/{targetVersion}.pwr
butler apply --staging-dir <staging> <patch.pwr> <gameDir>
```
**MVP 可不做**，全量下载即可（Minecraft 侧也是全量）。引入 butler 是独立决策。

### 3.6 模组模型（与 Minecraft 镜像反转）

| 维度 | Minecraft | Hytale |
|------|-----------|--------|
| 执行位置 | 客户端 + 服务端 | **仅服务端** |
| 客户端需要装模组 | 通常需要 | **不需要，服务器推送资产包** |
| 模组加载器 | Forge/Fabric/Quilt/NeoForge | **无加载器，JAR 直接丢 `UserData/Mods/`** |
| 模组形态 | 复杂的客户端 mod | 服务端 Java 插件（`.jar` + `manifest.json`） |

插件 `manifest.json` 示例：
```json
{
  "Group": "com.example",
  "Name": "MyPlugin",
  "Version": "1.0.0",
  "Main": "com.example.MyPlugin",
  "ServerVersion": ">=0.0.1",
  "Dependencies": { "Hytale:SomePlugin": ">=1.0.0" }
}
```

### 3.7 托管站与清单 API

| 平台 | API | gameId / 认证 | 对接友好度 |
|------|-----|---------------|-----------|
| **CurseForge** | REST | gameId=70216（Minecraft 是 432）；Header `x-api-key` | ⭐ API 与 Minecraft 版完全相同，Polymerium 已对接，复用成本极低 |
| **Modtale** | 开源 REST（AGPLv3） | Bearer token | ⭐ 可自部署 |
| UnifiedHytale | Partner REST + OAuth | 付费模组场景 | 门槛较高，非 MVP |
| Modrinth | ❌ 拒绝 | — | 不可用（issue #5025 closed-not-planned） |

**来源**：docs.curseforge.com/rest-api、modtale.net/api-docs、github.com/Modtale/modtale、github.com/modrinth/code/issues/5025

### 3.8 Persist 机制适用性：✅ 天作之合

Polymerium persist 机制（用户文件跨更新存活，通过 symlink 接入 build 目录）的语义，与 Hytale 的 `--user-dir` 能力天然契合：

- persist 管的是「跨更新要保留的用户文件」 → Hytale 里就是 **`UserData/`（Saves/Mods/settings/logs）**
- `--user-dir` 能把 UserData 指向实例目录 → persist 可直接把 UserData 当管辖范围
- Hytale 没有 Minecraft 那种「assets/libraries/客户端 jar 全是部署产物」的复杂模型 → persist 在 Hytale 侧反而更简单
- persist 机制本身经核查 🟢 完全通用（不读内容、不假设语义）

Hytale 实例落地形态：

```
{实例目录}/{key}/
├── build/                          # 运行时目录
│   ├── Client/  →  symlink 到共享层 (game/latest/Client)
│   ├── jre/     →  symlink 到共享层 (jre/latest)
│   └── ...
├── persist/                        # Polymerium persist 管辖区
│   └── UserData/                   # 用户 Saves/Mods/settings/logs
│       └── (启动时 --user-dir 指向这里)
├── snapshots/                      # 快照直接复用
└── profile.json / data.lock.json
```

启动时 `--user-dir {实例}/persist/UserData`、`--app-dir {共享层}/game/latest`，完美契合 persist 的「用户文件独立 + 可执行物共享」模型。

### 3.9 已知坑

- **Windows Mutex 互斥锁**：HytaleClient 用 Windows Mutex 防多开。社区靠关 mutex handle 的外部工具实现双开。限制「Polymerium 里同时跑两个 Hytale 实例」，但单实例隔离部署不受影响。
- **动态库搜索路径**：Linux 上要注入 `LD_LIBRARY_PATH` 指向 Client 目录（symlink 解析后的真实路径）。
- **macOS 官方支持空白**：官方仅 Flatpak + Windows，macOS 是 Polymerium 的差异化机会，但需自行验证 HytaleClient 在 macOS 上的可用性（早期 EA 阶段，社区启动器大多未覆盖 macOS）。

---

## 4. 现有机制复用核查（经源码核查，非推断）

> 针对「persist/import/modpack 不能复用就没意义」这个顾虑的逐条核查结论。

| 机制 | 解决什么 | 关键实现 | 耦合度 | Hytale 复用 |
|------|----------|----------|--------|-------------|
| **persist** 目录 | 用户文件跨更新存活 | `GenerateManifestStage.cs:105-113`、`SolidifyManifestStage.cs:28-356`、`PathDef:88` | 🟢 完全通用 | 直接复用，零改动 |
| **import** 目录 | modpack 覆盖文件暂存区 | `ImporterAgent.cs:25-26`、`GenerateManifestStage.cs:103-111`、`PathDef:86` | 🟡 目录机制通用 | 目录直接复用 |
| **modpack 导入** | 从 zip 解析并创建实例 | `IProfileImporter`（接口通用）+ 4 个 Minecraft importer（loader 映射硬编码） | 🔴 解析实现耦合 / 🟢 框架通用 | 框架直接复用，新增 Hytale importer 实现 |
| **snapshot** 快照 | 实例文件哈希快照 + 版本管理 | `SnapshotManager`、`SnapshotStore`、`PathDef:89-93` | 🟢 完全通用 | 直接复用，零改动 |
| **SimpleViewStatePersistence** | ViewModel UI 状态持久化到 SQLite | `Facilities/SimpleViewStatePersistence.cs` | 🟢 无关游戏 | 与 persist 目录无关，直接复用 |

**结论**：四个机制里三个可直接复用，剩下的 modpack 导入是框架复用 + 新增实现。Polymerium 现有架构的复用底盘足够厚，加 Hytale 有意义。

---

## 5. 架构设计

### 5.1 核心抽象：`GameKind` 一等公民

当前代码隐含假设「世界只有 Minecraft」，无 `GameType` 概念。引入显式的游戏类型标识作为所有游戏相关分发的根判据。

```csharp
// 新增：submodules/Trident.Net/src/TridentCore.Abstractions/Games/GameKind.cs
public readonly record struct GameKind(string Value)
{
    public static readonly GameKind Minecraft = new("minecraft");
    public static readonly GameKind Hytale    = new("hytale");
    public static readonly GameKind Unknown   = default;
}
```

采用 `record struct` 而非 enum，未来加游戏不用改枚举、不用改 switch，可由插件按需扩展。

### 5.2 `Profile.Rice` 增加游戏类型字段

`submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/Profile.cs`

```csharp
public class Rice
{
    public string? Source { get; set; }
    public required string Version { get; set; }
    public string? Loader { get; set; }
    public GameKind Game { get; set; } = GameKind.Minecraft;  // 新增，默认 Minecraft 保持向后兼容
    public IList<Entry> Packages { get; init; } = [];
    public IList<Rule> Rules { get; init; } = [];
}
```

默认值保证现有 Minecraft profile 反序列化不破坏。`Version`/`Loader` 字段语义保持不变（字符串），由各游戏的解释器赋予含义。

### 5.3 部署产物模型多态化（最硬的雷）

现状 `LockData` 是 JVM 专属产物：`MainClass`、`JavaMajorVersion`、`GameArguments`、`JavaArguments`、`Libraries`、`AssetIndex`。Hytale 的产物是原生 exe + 内嵌 JRE + 资源包，物理形态根本不同。

**设计**：`LockData` 改为可扩展的产物容器，按 `GameKind` 携带不同形态的产物 payload。

```csharp
// submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/LockData.cs
public record LockData(
    ViabilityData Viability,
    GameKind Game,                    // 新增
    ILockPayload Payload              // 新增：多态产物
);

public interface ILockPayload { }     // 标记接口

// Minecraft 现有字段搬到这里
public record MinecraftPayload(
    string MainClass,
    uint JavaMajorVersion,
    IReadOnlyList<string> GameArguments,
    IReadOnlyList<string> JavaArguments,
    IReadOnlyList<Library> Libraries,
    IReadOnlyList<Parcel> Parcels,
    AssetData AssetIndex
) : ILockPayload;

// Hytale 新增
public record HytalePayload(
    string ClientExecutablePath,      // HytaleClient(.exe) 路径
    string JavaExecutablePath,        // JRE java 路径
    string GameDir,                   // --app-dir 目标
    string UserDataDir,               // --user-dir 目标
    IReadOnlyList<string> ExtraArgs,
    IReadOnlyList<Parcel> Parcels
) : ILockPayload;
```

**迁移成本**：`LockDataBuilder` 的现有方法（`SetMainClass`/`AddLibrary`/...）改为操作 `MinecraftPayload` 的 builder；所有读 `lockData.MainClass` 的代码改为读 `(lockData.Payload as MinecraftPayload)?.MainClass` 或模式匹配。这是一次性的机械迁移，影响面集中在启动引擎和部署 stage。

### 5.4 部署 stage 按游戏分发

现状 `DeployEngine` 按 `DeployStage` 枚举线性迭代 8 个固定 stage，其中 4 个是 Minecraft 专属逻辑。

**设计**：引入 `IDeployPlanner`，按 `GameKind` 返回该游戏的 stage 序列。`DeployEngine` 不再硬编码枚举，改为消费 planner 产出的 stage 列表。

```csharp
// submodules/Trident.Net/src/TridentCore.Abstractions/Engines/IDeployPlanner.cs
public interface IDeployPlanner
{
    GameKind Game { get; }
    IEnumerable<DeployStage> Plan(DeployContext context);
}

// Minecraft planner：保持现有 8 stage 顺序
public class MinecraftDeployPlanner : IDeployPlanner { ... }

// Hytale planner：自己的 stage 组合（可能只有 CheckArtifact/ResolvePackage/SolidifyManifest）
public class HytaleDeployPlanner : IDeployPlanner { ... }
```

`CheckArtifact`、`BuildArtifact`、`SolidifyManifest` 三个通用 stage 保持不变；`InstallVanilla`/`ProcessLoader`/`EnsureRuntime`/`GenerateManifest` 四个 Minecraft 专属 stage 由 Minecraft planner 引用，Hytale planner 用自己的等价 stage。

**通用 stage 留在 `Stages/` 公共目录；游戏专属 stage 放 `Stages/Minecraft/`、`Stages/Hytale/` 子目录。**

### 5.5 启动引擎多态化

现状 `Igniter`/`LaunchEngine` 基于「`java` 启动一个 mainClass」的 JVM 模型。Hytale 是「启动原生 exe」，是两种进程模型。

**设计**：引入 `ILaunchStrategy`，按 `GameKind` 生成进程启动信息。

```csharp
public interface ILaunchStrategy
{
    GameKind Game { get; }
    LaunchDescriptor Build(ILockPayload payload, IAccount account, LaunchOptions options);
}

public record LaunchDescriptor(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> EnvironmentVariables,
    string WorkingDirectory
);
```

Minecraft strategy 复用现有 `Igniter` 逻辑（构造 `java -cp ... MainClass`）；Hytale strategy 构造 `HytaleClient --app-dir ... --user-dir ... --java-exec ...`，并在 Linux 上注入 `LD_LIBRARY_PATH`。

### 5.6 运行时来源多态化

现状 `EnsureRuntimeStage` 绑定 Mojang Piston 的 JRE 代号。Hytale 用 Temurin 25，走 `launcher.hytale.com/jre.json`。

**设计**：引入 `IRuntimeResolver`，按 `GameKind` 决定运行时来源。

```csharp
public interface IRuntimeResolver
{
    GameKind Game { get; }
    Task<RuntimeResolution> ResolveAsync(string versionIdentifier, CancellationToken ct);
}
```

Minecraft resolver 调 Mojang Piston；Hytale resolver 调 `launcher.hytale.com/jre.json`。

### 5.7 模组来源：复用 `IRepository`，新增 Hytale 实现

`IRepository`（`TridentCore.Abstractions/Repositories/IRepository.cs`）框架通用，经核查可复用。

**改动**：
- `CurseForgeHelper.GAME_ID` 从常量 `432` 改为可注入参数（或由 Repository 实例持有）。
- 新增 `HytaleCurseForgeRepository`：同一套 CurseForge 客户端，gameId=70216。
- 可选：新增 `ModtaleRepository` 对接 Modtale API。
- `RepositoryAgent` 按 profile 的 `GameKind` 选择对应的 Repository 集合。

### 5.8 Modpack 导入：复用 `IProfileImporter`，新增 Hytale importer

`IProfileImporter` + `ImporterAgent` 分发框架通用，4 个现有 importer 是 Minecraft 专属。

**改动**：新增 `HytaleImporter`（若 Hytale 社区出现标准 modpack 格式，或 Polymerium 自定义一个基于 `trident.index.json` 的格式）。Hytale 早期 EA 阶段 modpack 生态不成熟，此条优先级可后置。

### 5.9 账号系统：复用 `IAccount`，新增 Hytale OAuth

`IAccount` 通用，认证链通过 DI 的 `AddAccountConfigurers()` 注册。

**改动**：新增 `HytaleAccountConfigurer` 实现 OAuth Device Flow（§3.3），产物为 `HytaleAccount : IAccount`（持有 access_token / refresh_token）。注册到 DI 时与 Microsoft/Xbox 链并存。

### 5.10 Hytale 共享层（差异化设计，比官方启动器更省空间）

官方启动器每个 channel 全量独立副本（4 channel = 4 份 JRE + 4 份游戏）。Polymerium 利用现有 persist/symlink 机制做共享层：

```
{Polymerium 私有目录}/hytale-shared/
├── release/
│   ├── game/latest/        # 全局唯一副本
│   └── jre/latest/
└── beta/...
```

每个 Hytale 实例的 `build/` 目录通过 symlink 指向共享层，UserData 通过 `--user-dir` 指向各自 `persist/UserData/`。这是 Polymerium 的差异化优势。

### 5.11 UI 配置版面按游戏类型多态

**现状**（经源码核查）：游戏配置项散落在两处，共享同一套 JVM/Minecraft schema：

- **全局** `Pages/SettingsPage.axaml:324-405` 的「Game Defaults」section：`JavaMaxMemory` / `JavaAdditionalArguments` / `CommandWrapper` / `WindowInitialWidth/Height`
- **实例级** `Pages/InstancePropertiesPage.axaml:166-206`：同名四项带 `Override` 后缀（`JavaMaxMemoryOverride` 等），作为单实例覆盖

这四项对应 `Profile.Overrides` 的 `OVERRIDE_JAVA_MAX_MEMORY` / `OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS` / `OVERRIDE_BEHAVIOR_COMMAND_WRAPPER` / `OVERRIDE_WINDOW_WIDTH/HEIGHT`，全部是 JVM/Minecraft 专属语义。

**问题**：Hytale 客户端是原生 exe，`JavaMaxMemory`/`JavaAdditionalArguments`/`WindowSize` 对它无意义——Java 只服务于内嵌服务器且内存由原生进程自管，窗口由 HytaleClient 自管。直接复用这套 schema 会给用户呈现一堆无效配置项。

**设计**：配置版面按 `GameKind` 多态。引入「配置项描述」机制，每种游戏声明自己的可配置项集合，UI 据此渲染对应版面，而非把所有游戏的项硬编码进同一张表：

- **Minecraft schema**：保持现有四项（Java 内存/参数、命令包装、窗口大小）
- **Hytale schema**：自己的配置项——如 patchline/channel 选择（release/beta/alpha）、`--auth-mode`、额外 HytaleClient 启动参数、native lib 路径注入开关（`LD_LIBRARY_PATH`）等

**落地形态**：

- 全局 `SettingsPage` 的 Game Defaults section 按游戏分组（tab 或分段标题），每组渲染该游戏的 schema
- 实例级 `InstancePropertiesPage` 按实例 `Profile.Rice.Game` 渲染对应 schema 的 Override 项
- 配置值仍走 `Profile.Overrides` 的键值机制，但键空间按游戏分区（如 `hytale.patchline`、`hytale.extra_args`），与现有 `java.*` / `window.*` 键并列

这与 §5.3 `ILockPayload` 多态、§5.5 `ILaunchStrategy` 多态方向一致——产物、启动、配置 UI 三层都按游戏类型多态。用户在 Hytale 实例上看到的配置版面是 Hytale 专属的，而非套着 Minecraft JVM 参数的空壳；这正是「如果是 Hytale 实例就把 Game Defaults 整个版面换成 Hytale 的」这一诉求的工程落点。

---

## 6. 实施路线

按「先打地基（通用抽象），再插第一游戏（Minecraft 迁移），最后插第二游戏（Hytale 接入）」的顺序，保证每一步都可独立验证、不破坏现有 Minecraft 体验。

| 阶段 | 内容 | 依赖 | 风险 |
|------|------|------|------|
| **P0 地基** | 引入 `GameKind`；`Profile.Rice` 加 `Game` 字段（默认 Minecraft）；`LockData` 多态化（`ILockPayload`）；部署 stage 分目录；`DeployEngine` 改为消费 `IDeployPlanner` | 无 | 中 — `LockData` 字段搬动是机械迁移，影响启动引擎和所有 stage |
| **P1 Minecraft 迁移** | 现有 Minecraft 逻辑搬进 `MinecraftDeployPlanner` / `MinecraftLaunchStrategy` / `MinecraftRuntimeResolver` / `MinecraftPayload`；`CurseForgeHelper.GAME_ID` 参数化；Game Defaults 配置 schema 抽象化（§5.11），版面可按游戏切换 | P0 | 中 — 必须保证 Minecraft 全功能回归不破 |
| **P2 Hytale 核心** | `HytaleDeployPlanner` / `HytaleLaunchStrategy` / `HytaleRuntimeResolver` / `HytalePayload`；共享层布局（§5.10）；Hytale OAuth + Game Assets API 对接；Hytale 配置版面（§5.11） | P1 | 高 — 启动参数/JRE 发现/dynamic lib path 需实测验证 |
| **P3 Hytale 模组** | `HytaleCurseForgeRepository`（gameId=70216）；可选 `ModtaleRepository`；UI 适配 Hytale 模组展示 | P2 | 低 — CurseForge API 已验证同构 |
| **P4 Hytale modpack（可选）** | `HytaleImporter`，待 Hytale 社区 modpack 格式成熟 | P2 | 低 — 生态早期，可后置 |
| **P5 增量更新（可选）** | 引入 butler + `.pwr` 补丁支持 | P2 | 中 — 外部依赖，非 MVP |

---

## 7. 待确认问题（需用户拍板）

1. **macOS Hytale 支持优先级**：官方无 macOS 客户端，HytaleClient 在 macOS 上能否跑通未验证。是先做 Win/Linux（官方支持的平台），还是把 macOS 作为差异化重点尽早验证？
2. **Hytale 是否要支持多开**：受 Windows Mutex 限制。是否引入社区 mutex 关闭方案，还是接受单实例限制？
3. **模组来源范围**：MVP 只接 CurseForge（最成熟），还是同时接 Modtale（开源友好）？
4. **共享层是否做 channel 隔离**：Hytale 有 release/beta/alpha/dev 四个 channel，共享层是否按 channel 分目录？还是 MVP 只支持 release？
5. **是否引入 butler 增量更新**：全量下载对早期 EA 足够，增量更新是后续优化。
6. **实施时序与排期**：P0–P2 是大改，是否需要拆 PR 分阶段合入？还是一次性完成架构改造？

---

## 8. 参考资源

### Hytale 外部
- **HyTaLauncher**（C#/.NET 8 MIT，移植蓝本）：github.com/MerryJoyKey-Studio/HyTaLauncher
- **官方启动器逆向**：github.com/decomp-project/hytale-launcher
- **下载器逆向**：github.com/decomp-project/hytale-downloader
- **文件布局实测 Gist**：gist.github.com/karol-broda/3491df132865ffb3a3cddcde1848fdd9
- **HytaleDB 非官方技术文档**：hytaledb.ginco.gg
- **CurseForge API**：docs.curseforge.com/rest-api
- **Modtale API**：modtale.net/api-docs
- **Hytale 服务端 API Javadoc**：release.server.docs.hytale.com
- **HRS Launcher（Rust，独立 app-dir/user-dir 验证）**：github.com/RustedBytes/hrs-launcher

### Polymerium 内部核查依据
- `submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/Profile.cs` — `Rice` 数据模型
- `submodules/Trident.Net/src/TridentCore.Abstractions/FileModels/LockData.cs` — 部署产物模型
- `submodules/Trident.Net/src/TridentCore.Core/Engines/DeployEngine.cs` — 部署引擎
- `submodules/Trident.Net/src/TridentCore.Core/Engines/Deploying/DeployStage.cs` — stage 枚举
- `submodules/Trident.Net/src/TridentCore.Core/Engines/Deploying/Stages/*.cs` — 各 stage 实现
- `submodules/Trident.Net/src/TridentCore.Abstractions/Importers/IProfileImporter.cs` — modpack 导入抽象
- `submodules/Trident.Net/src/TridentCore.Abstractions/Repositories/IRepository.cs` — 模组来源抽象
- `submodules/Trident.Net/src/TridentCore.Abstractions/Accounts/IAccount.cs` — 账号抽象
- `submodules/Trident.Net/src/TridentCore.Abstractions/PathDef.cs:86-93` — import/persist/snapshots 目录定义
