# Authlib-Injector 外置登录实施方案

## 目标

为 Polymerium/Trident.Net 新增 Authlib-Injector 外置验证账号支持，使用户能够通过自定义 Yggdrasil 协议服务器（如 LittleSkin、自定义皮肤站等）进行身份验证并启动游戏。

---

## 整体架构

```
┌─ Deploy 阶段 ──────────────────────────────────────────────┐
│                                                            │
│  InstallVanillaStage                                       │
│    └─ AuthlibInjectorService.GetLatestAsync()              │
│       └─ GET /artifact/latest.json                         │
│    └─ builder.AddLibrary(                                  │
│         "moe.yushi.authlib-injector:...",                  │
│         downloadUrl, sha256,                               │
│         IsPresent=false ← 下载但不进 classpath             │
│       )                                                    │
│                                                            │
│  → SolidifyManifestStage 自动下载（已缓存则跳过）          │
│  → LockData JSON 持久化                                    │
│                                                            │
└────────────────────────────────────────────────────────────┘

┌─ Launch 阶段 ──────────────────────────────────────────────┐
│                                                            │
│  InstanceManager.LaunchCoreAsync                           │
│    └─ foreach (var arg in Account.ExtraJvmArguments)       │
│         igniter.AddJvmArgument(arg);                       │
│                                                            │
│  AuthlibInjectorAccount.ExtraJvmArguments →                │
│    -javaagent:<library-path>                               │
│    -Dauthlibinjector.yggdrasil=<server-url>                │
│                                                            │
└────────────────────────────────────────────────────────────┘

┌─ Account 认证流 ───────────────────────────────────────────┐
│                                                            │
│  AuthlibInjectorAccount                                    │
│    └─ YggdrasilAuthClient                                  │
│         ├─ POST /authserver/authenticate  → 获取 Token     │
│         ├─ POST /authserver/refresh       → 刷新 Token     │
│         ├─ POST /authserver/validate      → 验证 Token     │
│         └─ GET  /sessionserver/session/minecraft/profile   │
│                                    → 获取 UUID + 皮肤      │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## 改动清单

### Phase 1: 核心能力（Trident.Net）

#### 1.1 `IAccount` 扩展

**文件**: `submodules/Trident.Net/src/TridentCore.Abstractions/Accounts/IAccount.cs`

新增带默认实现的属性，对现有三种账号类型零影响：

```csharp
namespace TridentCore.Abstractions.Accounts;

public interface IAccount
{
    string Username { get; }
    string Uuid { get; }
    string AccessToken { get; }
    string UserType { get; }

    /// <summary>
    /// 启动时需要额外注入的 JVM 参数。
    /// 默认为空集合，特定账号类型（如 Authlib-Injector）可重写。
    /// </summary>
    IReadOnlyList<string> ExtraJvmArguments => Array.Empty<string>();
}
```

---

#### 1.2 `AuthlibInjectorAccount` 新增

**文件**: `submodules/Trident.Net/src/TridentCore.Core/Accounts/AuthlibInjectorAccount.cs`（新建）

```csharp
using TridentCore.Abstractions;
using TridentCore.Abstractions.Accounts;

namespace TridentCore.Core.Accounts;

public class AuthlibInjectorAccount : IAccount
{
    /// <summary>
    /// 外置验证服务器根地址，如 https://littleskin.cn/api/yggdrasil
    /// </summary>
    public required string ServerUrl { get; init; }

    /// <summary>
    /// 用于刷新令牌的 refreshToken
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// AccessToken 过期时间
    /// </summary>
    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    #region IAccount Members

    public required string Username { get; init; }
    public required string Uuid { get; init; }
    public required string AccessToken { get; set; }
    public string UserType => "mojang";

    public IReadOnlyList<string> ExtraJvmArguments
    {
        get
        {
            var jarPath = PathDef.Default.FileOfLibrary(
                "moe.yushi.authlib-injector",
                "authlib-injector",
                _authlibVersion,
                null,
                "jar"
            );
            return [
                $"-javaagent:{jarPath}",
                $"-Dauthlibinjector.yggdrasil={ServerUrl}"
            ];
        }
    }

    #endregion

    /// <summary>
    /// 当前使用的 authlib-injector 版本号，用于定位 jar 文件路径。
    /// 由账号创建时从 AuthlibInjectorService 获取并固化。
    /// </summary>
    public required string AuthlibVersion { get; init; }
}
```

> **注意**：`AuthlibVersion` 必须作为账号属性持久化，因为 Launch 阶段需要用它定位 jar 路径。如果后续 authlib-injector 更新导致 jar 路径变化，旧账号仍能找到自己创建时的版本（该版本在 Deploy 时已被缓存到本地）。

> **`UserType` 的选择**：authlib-injector 官方文档建议使用 `"mojang"`，因为 Minecraft 客户端会将该值透传到 session server 验证请求中。如果外置服务器对 userType 有特殊要求可调整。

---

#### 1.3 Authlib-Injector 下载服务

**文件**: `submodules/Trident.Net/src/TridentCore.Core/Services/AuthlibInjectorService.cs`（新建）

```csharp
namespace TridentCore.Core.Services;

/// <summary>
/// authlib-injector 构件信息
/// </summary>
public record AuthlibInjectorArtifact(
    int BuildNumber,
    string Version,
    Uri DownloadUrl,
    string Sha256
);

public class AuthlibInjectorService(IHttpClientFactory httpClientFactory)
{
    // 可通过 DI 配置切换镜像源
    private const string DefaultBaseUrl = "https://authlib-injector.yushi.moe";

    private string _baseUrl = DefaultBaseUrl;

    public void SetBaseUrl(string url) => _baseUrl = url;

    /// <summary>
    /// 获取最新版本的 authlib-injector 构件信息
    /// GET /artifact/latest.json
    /// </summary>
    public async Task<AuthlibInjectorArtifact> GetLatestAsync(
        CancellationToken token = default)
    {
        using var client = httpClientFactory.CreateClient();
        var url = $"{_baseUrl}/artifact/latest.json";
        var json = await client.GetStringAsync(url, token).ConfigureAwait(false);
        var doc = JsonNode.Parse(json)
            ?? throw new InvalidOperationException("Invalid response from authlib-injector API");

        var buildNumber = doc["build_number"]?.GetValue<int>()
            ?? throw new FormatException("Missing build_number");
        var version = doc["version"]?.GetValue<string>()
            ?? throw new FormatException("Missing version");
        var downloadUrl = doc["download_url"]?.GetValue<string>()
            ?? throw new FormatException("Missing download_url");
        var sha256 = doc["checksums"]?["sha256"]?.GetValue<string>()
            ?? throw new FormatException("Missing checksums.sha256");

        return new(buildNumber, version, new(downloadUrl, UriKind.Absolute), sha256);
    }
}
```

**DI 注册** — 在 `Trident.Core` 的 `Startup` / DI 配置中添加：

```csharp
services.AddSingleton<AuthlibInjectorService>();
```

**镜像源支持**：BMCLAPI 镜像地址为 `https://bmclapi2.bangbang93.com/mirrors/authlib-injector`。可在 Polymerium.App 的 `Configuration` 或 `Startup.cs` 中通过 `authlibInjectorService.SetBaseUrl(...)` 配置。该配置项可以复用已有的代理/镜像设置机制。

---

#### 1.4 Deploy 阶段注入 authlib-injector Library

**文件**: `submodules/Trident.Net/src/TridentCore.Core/Engines/Deploying/Stages/InstallVanillaStage.cs`

在 `OnProcessAsync` 末尾新增 authlib-injector library 注册：

```csharp
// === 新增：注入 authlib-injector ===
var authlibService = Context.Options.ServiceProvider
    .GetRequiredService<AuthlibInjectorService>();
var authlibArtifact = await authlibService.GetLatestAsync(token)
    .ConfigureAwait(false);

builder.AddLibrary(
    $"moe.yushi.authlib-injector:authlib-injector:{authlibArtifact.Version}",
    authlibArtifact.DownloadUrl,
    authlibArtifact.Sha256,
    native: false,
    present: false  // 下载到本地缓存，但不加入 classpath
);
logger.LogInformation(
    "Authlib-injector {version} appended (build {build})",
    authlibArtifact.Version,
    authlibArtifact.BuildNumber
);
```

**关键设计**：`IsPresent=false` 确保：
- `GenerateManifestStage` 会将其加入下载清单（不跳过）
- `SolidifyManifestStage` 会下载并校验 sha256（已缓存则跳过）
- `MakeIgniter()` 的 `.Where(x => x.IsPresent)` 会将其排除出 classpath
- 最终由 `-javaagent:` JVM 参数引用，不经过 `-cp`

**关于 `ServiceProvider` 的获取**：需要确认 `DeployContext` 或 `DeployEngineOptions` 是否已经持有 `IServiceProvider`。如果没有，需要新增一个属性透传。备选方案是让 `InstallVanillaStage` 通过构造函数注入 `AuthlibInjectorService`（Trident 已使用 primary constructor 模式，直接加参数即可）。

---

#### 1.5 Launch 阶段注入 JVM 参数

**文件**: `submodules/Trident.Net/src/TridentCore.Core/Services/InstanceManager.cs`

在 `LaunchCoreAsync` 中，现有 `AdditionalArguments` 循环之后新增：

```csharp
// 现有代码（约 L395）:
foreach (var additional in options.AdditionalArguments.Split(' '))
{
    igniter.AddJvmArgument(additional);
}

// === 新增：注入账号所需的额外 JVM 参数 ===
foreach (var jvmArg in options.Account.ExtraJvmArguments)
{
    igniter.AddJvmArgument(jvmArg);
}
```

由于 `IAccount.ExtraJvmArguments` 默认返回空数组，对 `OfflineAccount`、`MicrosoftAccount`、`TrialAccount` 无任何影响。

---

#### 1.6 Yggdrasil 认证客户端

**文件**: `submodules/Trident.Net/src/TridentCore.Core/Services/YggdrasilAuthClient.cs`（新建）

实现标准 Yggdrasil 协议的认证流程：

```csharp
namespace TridentCore.Core.Services;

/// <summary>
/// Yggdrasil 协议认证响应
/// </summary>
public record YggdrasilAuthenticateResponse(
    string AccessToken,
    string ClientToken,
    IReadOnlyList<YggdrasilGameProfile> AvailableProfiles,
    YggdrasilGameProfile? SelectedProfile
);

public record YggdrasilGameProfile(
    string Id,    // UUID (无连字符)
    string Name   // 用户名
);

public record YggdrasilRefreshResponse(
    string AccessToken,
    string ClientToken,
    YggdrasilGameProfile? SelectedProfile
);

public class YggdrasilAuthClient(IHttpClientFactory httpClientFactory)
{
    /// <summary>
    /// 使用用户名密码认证
    /// POST {serverUrl}/authserver/authenticate
    /// </summary>
    public async Task<YggdrasilAuthenticateResponse> AuthenticateAsync(
        string serverUrl,
        string username,
        string password,
        string? clientToken = null,
        CancellationToken token = default)
    {
        // 请求体遵循 Yggdrasil 协议规范
    }

    /// <summary>
    /// 刷新访问令牌
    /// POST {serverUrl}/authserver/refresh
    /// </summary>
    public async Task<YggdrasilRefreshResponse> RefreshAsync(
        string serverUrl,
        string accessToken,
        string clientToken,
        CancellationToken token = default)
    {
        // ...
    }

    /// <summary>
    /// 验证令牌是否有效
    /// POST {serverUrl}/authserver/validate
    /// </summary>
    public async Task<bool> ValidateAsync(
        string serverUrl,
        string accessToken,
        string clientToken,
        CancellationToken token = default)
    {
        // ...
    }
}
```

**Yggdrasil 协议要点**（标准规范）：

| 端点 | 方法 | 用途 |
|------|------|------|
| `/authserver/authenticate` | POST | 用户名密码登录，获取 accessToken |
| `/authserver/refresh` | POST | 刷新即将过期的 accessToken |
| `/authserver/validate` | POST | 检查 accessToken 是否有效 |
| `/authserver/signout` | POST | 使用用户名密码注销 |
| `/authserver/invalidate` | POST | 使用 token 注销 |

**authenticate 请求体**：
```json
{
  "username": "邮箱或用户名",
  "password": "密码",
  "clientToken": "UUID（可选，服务端会生成）",
  "requestUser": true
}
```

**authenticate 响应体**：
```json
{
  "accessToken": "基底64或UUID格式的令牌",
  "clientToken": "客户端令牌",
  "availableProfiles": [
    { "id": "无连字符UUID", "name": "玩家名" }
  ],
  "selectedProfile": { "id": "...", "name": "..." }
}
```

> **注意**：部分外置服务器使用邮箱作为 username，部分使用纯用户名。API 客户端不应假设格式，直接透传用户输入。

---

### Phase 2: UI 集成（Polymerium.App）

#### 2.1 账号创建组件

**文件**: `src/Polymerium.App/Components/AccountCreationAuthlib.axaml` + `.axaml.cs`（新建）

参照现有 `AccountCreationMicrosoft.axaml` 的模式，创建新的账号创建向导步骤：

```
Step 1: 输入外置验证服务器地址
        ┌──────────────────────────────────┐
        │  服务器地址                       │
        │  [https://littleskin.cn/api/ygg] │
        │                                  │
        │  常用服务器:                      │
        │  • LittleSkin                    │
        │  • 自定义...                      │
        └──────────────────────────────────┘

Step 2: 输入账号密码
        ┌──────────────────────────────────┐
        │  邮箱 / 用户名                    │
        │  [________________]              │
        │  密码                             │
        │  [________________]              │
        │         [登录]                    │
        └──────────────────────────────────┘

Step 3: 确认（复用 AccountCreationPreview）
        显示: 服务器地址、用户名、UUID
```

#### 2.2 账号创建流程

**文件**: `src/Polymerium.App/Components/AccountCreationAuthlib.axaml.cs`

核心流程：
1. 用户输入服务器地址 → 可选：测试连通性（`GET {serverUrl}/` 应返回 Yggdrasil 元数据）
2. 用户输入用户名密码 → 调用 `YggdrasilAuthClient.AuthenticateAsync`
3. 获取到 `selectedProfile` 后 → 调用 `AuthlibInjectorService.GetLatestAsync` 获取当前版本号
4. 构建 `AuthlibInjectorAccount` 并保存到账号存储

#### 2.3 账号模型扩展

**文件**: `src/Polymerium.App/Models/AccountModel.cs`

在现有的账号模型枚举中新增 `AuthlibInjector` 类型，确保：
- 账号列表能正确展示外置账号
- 账号详情能显示服务器地址
- 账号刷新能调用 `YggdrasilAuthClient.RefreshAsync`

#### 2.4 本地化

**文件**: `src/Polymerium.App/Properties/Resources.resx` + `Resources.zh-hans.resx`

新增字符串：
- `AccountCreation_Authlib_Title` / "外置登录 (Authlib-Injector)"
- `AccountCreation_Authlib_ServerAddress` / "服务器地址"
- `AccountCreation_Authlib_Username` / "邮箱 / 用户名"
- `AccountCreation_Authlib_Password` / "密码"
- `AccountCreation_Authlib_LoginFailed` / "登录失败：{0}"
- `AccountType_AuthlibInjector` / "外置验证"

#### 2.5 账号刷新/失效处理

在现有账号刷新逻辑（`MainWindowContext` 或账号管理服务）中，增加对 `AuthlibInjectorAccount` 的处理：
- 检测 `AccessTokenExpiresAt`，若即将过期则调用 `YggdrasilAuthClient.RefreshAsync`
- 若 refresh 失败，标记账号为失效状态，提示用户重新登录

---

## 数据流详解

### Deploy 流程（authlib-injector jar 下载）

```
InstallVanillaStage.OnProcessAsync()
  │
  ├─ ... 现有 vanilla libraries/arguments 处理 ...
  │
  └─ AuthlibInjectorService.GetLatestAsync()
       │ GET https://authlib-injector.yushi.moe/artifact/latest.json
       │ Response: { version: "1.2.x", download_url: "...", checksums: { sha256: "..." } }
       │
       └─ builder.AddLibrary(
            "moe.yushi.authlib-injector:authlib-injector:1.2.x",
            downloadUrl, sha256,
            IsPresent=false
          )
            │
            ▼ 写入 LockData
            │
GenerateManifestStage.OnProcessAsync()
  │ 遍历 artifact.Libraries（不区分 IsPresent）
  └─ manifest.PresentFiles.Add(
       FileOfLibrary("moe.yushi.authlib-injector", "authlib-injector", "1.2.x", null, "jar"),
       downloadUrl, sha256
     )
            │
            ▼
SolidifyManifestStage.OnProcessAsync()
  │ 检查本地文件 sha256
  ├─ 已缓存且匹配 → 跳过
  └─ 未缓存或不匹配 → 下载到
       libraries/moe/yushi/authlib-injector/1.2.x/authlib-injector-1.2.x.jar
```

### Launch 流程（JVM 参数注入）

```
InstanceManager.LaunchCoreAsync()
  │
  ├─ artifact.MakeIgniter()
  │    └─ 只加载 IsPresent=true 的 library 到 classpath
  │       authlib-injector 不在 classpath ✅
  │
  ├─ igniter.SetUserUuid(account.Uuid)
  ├─ igniter.SetUserType(account.UserType)    // "mojang"
  ├─ igniter.SetUserName(account.Username)
  ├─ igniter.SetUserAccessToken(account.AccessToken)
  │
  └─ foreach (var arg in account.ExtraJvmArguments)
       │ AuthlibInjectorAccount 返回:
       │   -javaagent:libraries/moe/yushi/authlib-injector/1.2.x/authlib-injector-1.2.x.jar
       │   -Dauthlibinjector.yggdrasil=https://littleskin.cn/api/yggdrasil
       └─ igniter.AddJvmArgument(arg)
            │
            ▼
       Process: java
         -javaagent:...authlib-injector-1.2.x.jar
         -Dauthlibinjector.yggdrasil=https://...
         ... 其他 JVM 参数 ...
         -cp classpath MainClass
         ... game arguments (${auth_access_token}, ${auth_uuid} 等) ...
```

### 版本更新机制

```
1. authlib-injector 发布新版本 1.3.0
2. 用户触发 Deploy（新建实例/修改配置/清除缓存）
3. InstallVanillaStage 请求 /artifact/latest.json → version: "1.3.0"
4. builder.AddLibrary(... "1.3.0" ...)
5. LockData 中的 library 版本变化 → ViabilityData.RulesHash 变化
6. CheckArtifactStage 校验失败 → 触发完整重新 Deploy
7. 新 jar 被下载，旧 jar 保留在磁盘（不删除，可能有旧账号引用）
```

---

## 改动文件汇总

| 文件 | 操作 | 说明 |
|------|------|------|
| `TridentCore.Abstractions/Accounts/IAccount.cs` | **改** | 新增 `ExtraJvmArguments` 属性 |
| `TridentCore.Core/Accounts/AuthlibInjectorAccount.cs` | **新建** | 外置验证账号实现 |
| `TridentCore.Core/Services/AuthlibInjectorService.cs` | **新建** | authlib-injector API 客户端 |
| `TridentCore.Core/Services/YggdrasilAuthClient.cs` | **新建** | Yggdrasil 协议认证客户端 |
| `TridentCore.Core/Models/YggdrasilApi/*.cs` | **新建** | 请求/响应模型 |
| `TridentCore.Core/Engines/Deploying/Stages/InstallVanillaStage.cs` | **改** | 注入 authlib-injector library |
| `TridentCore.Core/Services/InstanceManager.cs` | **改** | 遍历 ExtraJvmArguments |
| `Polymerium.App/Components/AccountCreationAuthlib.axaml` | **新建** | UI 布局 |
| `Polymerium.App/Components/AccountCreationAuthlib.axaml.cs` | **新建** | UI 逻辑 |
| `Polymerium.App/Models/AccountModel.cs` | **改** | 新增 AuthlibInjector 类型 |
| `Polymerium.App/Properties/Resources.resx` | **改** | 英文字符串 |
| `Polymerium.App/Properties/Resources.zh-hans.resx` | **改** | 中文字符串 |

---

## 风险与注意事项

1. **ServiceProvider 可用性**：`InstallVanillaStage` 需要 `AuthlibInjectorService`。当前 Stage 通过 `StageBase` 基类持有 `Context`，需要确认是否能从 Context 取到 ServiceProvider，还是需要直接在 Stage 构造函数中注入。优先使用构造函数注入（与 `ProcessLoaderStage` 注入 `PrismLauncherService` 的模式一致）。

2. **authlib-injector 版本与账号绑定**：`AuthlibInjectorAccount` 持有 `AuthlibVersion` 用于定位 jar 路径。当新版本部署后，旧账号仍指向旧版本 jar。磁盘上会累积多个版本的 jar，这合理（类似多个版本的 Minecraft library 共存）。

3. **API 不可用时的容错**：如果 `/artifact/latest.json` 请求失败，应检查本地是否已有缓存的 authlib-injector jar。如果有，使用本地最新版本继续；如果没有，抛出明确异常告知用户。

4. **皮肤加载**：authlib-injector 通过 `-Dauthlibinjector.yggdrasil` 参数劫持 Minecraft 的 texture profile 请求，无需额外代码。只要服务器 URL 正确，皮肤/披风自动生效。

5. **安全考虑**：密码不应持久化。只存储 `accessToken` + `refreshToken`。`refreshToken` 的存储安全性应与 `MicrosoftAccount.RefreshToken` 保持一致。

6. **客户端令牌 (clientToken)**：Yggdrasil 协议中 clientToken 用于标识客户端实例。建议为每个账号生成一个 UUID 并持久化，用于 refresh 和 validate 操作。

---

## 实施顺序

```
1. IAccount 接口扩展（ExtraJvmArguments）
2. AuthlibInjectorService（API 客户端）
3. InstallVanillaStage 注入 library
4. InstanceManager 注入 JVM 参数
5. 验证：此时 Deploy + Launch 应该能正确下载 jar 并注入参数（用 OfflineAccount 手动指定 -javaagent 测试）
6. YggdrasilAuthClient（认证客户端）
7. AuthlibInjectorAccount（账号实现）
8. 账号存储/刷新逻辑
9. Polymerium.App UI 组件
10. 本地化
```
