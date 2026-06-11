# authlib-injector 外置登录接入计划

## 概述

为 Polymerium 接入 authlib-injector 外置登录（Yggdrasil 协议）账号类型，包括核心库层、部署/启动管线、UI 界面三个阶段。

---

## 已完成：核心层 + 部署/启动管线

### 新增文件（Trident.Net 子模块）

| 文件 | 说明 |
|------|------|
| `src/TridentCore.Core/Clients/IAuthlibInjectorClient.cs` | Refit 客户端，对接 `authlib-injector.yushi.moe` 的版本 API（`/artifact/latest.json` 等） |
| `src/TridentCore.Core/Models/AuthlibInjectorApi/AuthlibInjectorArtifactListResponse.cs` | 版本列表响应模型 |
| `src/TridentCore.Core/Models/AuthlibInjectorApi/AuthlibInjectorArtifactResponse.cs` | 单版本响应模型（含 `download_url` + `sha256`） |
| `src/TridentCore.Core/Services/AuthlibInjectorService.cs` | 封装客户端，提供 `GetLatestAsync()` 及 Library Identity 常量（`moe.yushi:authlib-injector:{version}`） |
| `src/TridentCore.Core/Accounts/AuthlibInjectorAccount.cs` | 实现 `IAccount`，字段：`ServerUrl`/`Password`/`AccessToken`/`ClientToken`/`AccessTokenExpiresAt` |

### 修改文件（Trident.Net 子模块）

| 文件 | 改动 |
|------|------|
| `src/TridentCore.Core/Extensions/ServiceCollectionExtensions.cs` | 新增 `AddAuthlibInjector()` 方法，注册 Refit client + Service |
| `src/TridentCore.Core/Engines/Deploying/Stages/InstallVanillaStage.cs` | 注入 `AuthlibInjectorService`，deploy 时调 API 拿最新版，以 `IsPresent=false` 注册为 Library |
| `src/TridentCore.Core/Services/InstanceManager.cs` | `LaunchCoreAsync` 中检测 account 类型，从 `artifact.Libraries` 找到 authlib-injector 条目拼 `FileOfLibrary` 路径，追加 `-javaagent:{path}={serverUrl}` |

### 管线设计要点

1. **Deploy 阶段**：`InstallVanillaStage` 每次部署都调用 API 获取最新版 authlib-injector，作为 Library（`IsPresent=false`）写入 LockData，跟随现有下载/缓存机制。Library Identity 为 `moe.yushi:authlib-injector:{version}`，对应文件路径 `libraries/moe/yushi/authlib-injector/{version}/authlib-injector-{version}.jar`。

2. **Launch 阶段**：`InstanceManager.LaunchCoreAsync` 检测 `options.Account` 是否为 `AuthlibInjectorAccount`，若是则从 `artifact.Libraries` 中查找 authlib-injector 条目，用其 Identity 拼 `FileOfLibrary` 得到磁盘路径，追加 `-javaagent:{path}={serverUrl}` 到 JVM 参数。

3. 版本信息通过 LockData（`data.lock.json`）从 Deploy 传递到 Launch，无需重复查询 API。

---

## 待完成：Yggdrasil 认证服务

### 新增文件（Trident.Net 子模块）

| 文件 | 说明 |
|------|------|
| `src/TridentCore.Core/Clients/IYggdrasilClient.cs` | Refit 客户端，对接 Yggdrasil 协议认证端点 |
| `src/TridentCore.Core/Models/YggdrasilApi/*.cs` | 请求/响应模型 |

### IYggdrasilClient 端点

- `POST /authserver/authenticate` — 登录（返回 accessToken + clientToken + profiles）
- `POST /authserver/refresh` — 刷新令牌
- `POST /sessionserver/session/minecraft/profile/{uuid}` — 查询皮肤档案

### 注意事项

- BaseUrl 是动态的（每个用户填的认证服务器不同），不能用标准 Refit 固定 BaseAddress 模式。需要用 `IHttpClientFactory` 手动构建 `HttpClient`，或在 `YggdrasilService` 内部用 `RestService.For<IYggdrasilClient>(httpClient)` 动态创建。
- 参考项目内其他动态 BaseUrl 的做法（如果有），否则可参考 `GenerateManifestStage` 中 `IHttpClientFactory` 的使用方式。

### 新增文件

| 文件 | 说明 |
|------|------|
| `src/TridentCore.Core/Services/YggdrasilService.cs` | 封装认证/刷新逻辑，接收 `serverUrl` + `username` + `password`，返回 `AuthlibInjectorAccount` |

### 修改文件

| 文件 | 改动 |
|------|------|
| `src/TridentCore.Core/Extensions/ServiceCollectionExtensions.cs` | 注册 `YggdrasilService`（不需要注册 `IYggdrasilClient`，因为是动态 BaseUrl） |

### Yggdrasil 协议要点

请求体：
```json
{
  "agent": { "name": "Minecraft", "version": 1 },
  "username": "邮箱或用户名",
  "password": "密码",
  "clientToken": "客户端生成的UUID（可选，刷新时需要回传）",
  "requestUser": true
}
```

响应体：
```json
{
  "accessToken": "...",
  "clientToken": "...",
  "availableProfiles": [{ "id": "无横杠UUID", "name": "玩家名" }],
  "selectedProfile": { "id": "...", "name": "..." }
}
```

---

## 待完成：UI 层（Polymerium.Avalonia）

### 新增文件

| 文件 | 说明 |
|------|------|
| `src/Polymerium.Avalonia/Components/AccountCreationAuthlibInjector.axaml` | 添加账号的 UI：服务器地址、用户名、密码输入框，登录按钮 |
| `src/Polymerium.Avalonia/Components/AccountCreationAuthlibInjector.axaml.cs` | code-behind，调用 `YggdrasilService` 完成认证 |

### 修改文件

| 文件 | 改动 |
|------|------|
| `src/Polymerium.Avalonia/Components/AccountCreationPortal.axaml` | `AccountTypeSelectBox` 新增第四个 `TabStripItem`（authlib-injector） |
| `src/Polymerium.Avalonia/Components/AccountCreationPortal.axaml.cs` | `NextStep()` 增加 `case 3` 返回 `AccountCreationAuthlibInjector`，注入 `YggdrasilService` |
| `src/Polymerium.Avalonia/Modals/AccountCreationModal.axaml.cs` | 注入 `YggdrasilService` 并传递给 `AccountCreationPortal` |
| `src/Polymerium.Avalonia/PageModels/AccountsPageModel.cs` | 构造函数注入 `YggdrasilService`，传递给 `AccountCreationModal` |
| `src/Polymerium.Avalonia/Utilities/AccountHelper.cs` | `ToCooked` 增加 `AuthlibInjectorAccount` 反序列化分支 |
| `src/Polymerium.Avalonia/Properties/Resources.resx` | 添加英文本地化字符串 |
| `src/Polymerium.Avalonia/Properties/Resources.zh-hans.resx` | 添加中文本地化字符串 |
| `src/Polymerium.Avalonia/Startup.cs` | 调用 `AddAuthlibInjector()` + 注册 `YggdrasilService` |

### UI 组件参考

参照 `AccountCreationMicrosoft.axaml` 的模式：

- 继承 `AccountCreationStep`
- `IsNextAvailable` 在认证成功后设为 `true`
- `NextStep()` 返回 `AccountCreationPreview { Account = account }`
- required 属性注入 `YggdrasilService`

### AccountCreationAuthlibInjector UI 布局

```
┌─────────────────────────────┐
│ 服务器地址                    │
│ [________________________]  │
│                             │
│ 用户名 / 邮箱                │
│ [________________________]  │
│                             │
│ 密码                         │
│ [________________________]  │
│                             │
│ [登录]                       │
│                             │
│ (错误提示区)                  │
└─────────────────────────────┘
```

### 本地化字符串清单

| Key | en | zh-Hans |
|-----|----|---------|
| `AccountCreationPortal_AuthlibInjectorTitle` | External Auth | 外置登录 |
| `AccountCreationPortal_AuthlibInjectorSubtitle` | authlib-injector (Yggdrasil) | authlib-injector (外置验证) |
| `AccountCreationAuthlib_Title` | External Account | 外置账号 |
| `AccountCreationAuthlib_ServerUrl` | Server URL | 服务器地址 |
| `AccountCreationAuthlib_Username` | Username / Email | 用户名 / 邮箱 |
| `AccountCreationAuthlib_Password` | Password | 密码 |
| `AccountCreationAuthlib_Login` | Login | 登录 |
| `AccountCreationAuthlib_ErrorAuthFailed` | Authentication failed | 认证失败 |
| `AccountCreationAuthlib_ErrorInvalidServer` | Invalid server URL | 无效的服务器地址 |

---

## 待完成：AccountHelper.ToCooked 补丁

在 `AccountHelper.ToCooked` 的 switch 中增加：

```csharp
nameof(AuthlibInjectorAccount) => JsonSerializer.Deserialize<AuthlibInjectorAccount>(raw.Data),
```

---

## 实现顺序建议

1. Yggdrasil 认证服务（`IYggdrasilClient` + `YggdrasilService`）
2. UI 组件（`AccountCreationAuthlibInjector`）
3. Portal/Modal/PageModel 接入
4. AccountHelper + 本地化
5. Startup.cs DI 注册
6. 端到端测试
