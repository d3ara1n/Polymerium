# Modrinth Client API 大审核

> 制定日期：2026-07-05
> 定位：对 `submodules/Trident.Net` 的 `IModrinthClient` 全端点逐个比对 labrinth v3 源码，确认路由、参数名、参数值格式、运行时行为全部一致。
> 当前状态：草案

---

## 1. 背景与动机

2026-07 在 review Trident.Net PR #3 时，连续踩了两个静默失效的坑，暴露出 `IModrinthClient` 与 labrinth 实际实现之间存在系统性偏差，且这类偏差**不触发编译错误、只在运行时表现为"过滤没生效/返回不全/参数被忽略"**，极难发现：

1. **`game_versions` 参数在 v3 根本不存在。** PR 给 `GetProjectVersionsAsync` 加了 `[AliasAs("game_versions")]`，但 v3 把 game-version 过滤并进了 `loader_fields`（JSON 对象 `{"game_versions":[...]}`）。该参数被 API 静默忽略，又恰好删了客户端兜底过滤，导致 `Resolve*` 取到「最新版本，不管目标 game version」。实测确认：`/v3/project/sodium/version?game_versions=["1.16.5"]` 返回全部 199 个版本（不过滤），而 `loader_fields={"game_versions":["1.16.5"]}` 正确返回 2 个。
2. **labrinth 曾经的"先分页后过滤"bug。** version 列表接口旧实现是 `skip().take().filter()`，带 `limit=N` 时返回过滤前的前 N 个再筛，导致过滤后不足额。main 分支已修为 `filter().sort().skip().take()`，但说明**服务端实现细节会坑客户端**，且生产部署与 main 分支存在时间差。

更深层的诱因：

- `IModrinthClient` **混用 v2 和 v3 端点**（`GetGameVersionsAsync`/`GetProjectTypesAsync`/`SearchAsync` 走 v2，其余 v3）。v2 内部转发 v3 时会包装参数（如 v2 的 `game_versions` → v3 的 `loader_fields`，v2 的 `loaders` 额外追加 `"mrpack"`），同一参数名在 v2/v3 语义可能不同。
- **Refit 的 `PropertyNamingPolicy` 不影响 query 参数名**（Refit 官方确认，issue #1333）。query 参数名默认用 C# 参数名，v3 多为 snake_case，必须逐个 `[AliasAs]` 对齐，漏一个就静默失效。Modrinth client 的全局 naming（`RepositoryAgent.cs` 里配的 `SnakeCaseLower`）只管 JSON body/响应，救不了 query。
- Modrinth 的 mintlify 文档**与实现曾不一致**（v2 文档页至今还挂着 `game_versions`，正是这次踩坑的来源）。不能单独依赖文档。

结论：需要一次系统性的端到端审核，**源码 + 实测双验证**，把每个端点的路由、参数、行为都对一遍。

---

## 2. 目标 / 非目标 / 不做的事

### 目标

- `IModrinthClient` 的每个端点：路由路径、HTTP 方法、所有 path/query 参数的名与值格式，与 labrinth **当前生产源码**逐一核对一致。
- 每个带过滤/分页参数的端点**实测验证参数真正生效**（发真实请求，不只看源码/文档）。
- 产出一份审核记录（可附在本计划末尾或单独文件），列出每个端点的核对结论 + 已修/待修项。

### 非目标

- 不重构 client 架构（不换 HTTP 库、不引入新的抽象层、不改 `RepositoryAgent` 的 `RestService.For` 注册方式）。
- 不实现 labrinth 尚未稳定/未上线的端点。
- 不处理 CurseForge / Prism / Mojang 等其他 client（本计划只管 Modrinth；其他 client 可参照本计划的方法另起）。

### 不做的事

- **不把 v2 端点统一迁 v3** —— 除非审核发现某个 v2 端点已废弃或行为错误。迁移是独立决策，记录在案但不在本计划执行。
- **不改 Refit 全局 naming 方案** —— `PropertyNamingPolicy` 对 query 无效是 Refit 既定行为，靠 `[AliasAs]` 兜底是正确做法；若想全局化，需引入自定义 `IUrlParameterKeyFormatter`（snake_case），那是另一个任务。

---

## 3. 审核范围

`submodules/Trident.Net/src/TridentCore.Core/Clients/IModrinthClient.cs` 的 11 个方法：

| # | 方法 | 当前路由 | 关键参数 | 版本 | 备注 |
|---|------|----------|----------|------|------|
| 1 | `GetGameVersionsAsync` | `/v2/tag/game_version` | — | v2 | |
| 2 | `GetLoadersAsync` | `/v3/tag/loader` | — | v3 | |
| 3 | `GetProjectTypesAsync` | `/v2/tag/project_type` | — | v2 | |
| 4 | `SearchAsync` | `/v2/search` | query, facets, index, offset, limit | v2 | facets 是嵌套数组 JSON，格式易错 |
| 5 | `GetProjectAsync` | `/v3/project/{projectId}` | path | v3 | |
| 6 | `GetMultipleProjectsAsync` | `/v3/projects` | ids | v3 | ids 是 JSON 数组 |
| 7 | `GetVersionAsync` | `/v3/version/{versionId}` | path | v3 | |
| 8 | `GetMultipleVersionsAsync` | `/v3/versions` | ids | v3 | ids 是 JSON 数组 |
| 9 | `GetTeamMembersAsync` | `/v3/team/{teamId}/members` | path | v3 | |
| 10 | `GetProjectVersionsAsync` | `/v3/project/{projectId}/version` | version_type, loaders, loader_fields, include_changelog | v3 | **2026-07 已审**：修了 game_versions→loader_fields |
| 11 | `GetVersionFromHashAsync` | `/v3/version_file/{hash}` | algorithm | v3 | |

重点对象：**带 query 参数的端点**（#4 SearchAsync、#6/#8 的 ids、#10 已审、#11 algorithm）。纯 path 参数的端点风险低，但仍需确认路由存在。

---

## 4. 方法（分阶段）

### ⏳ 阶段 1：建立"真相基准"——拉 labrinth 源码

- clone `modrinth/code` monorepo（注意是 monorepo，不是旧的 `modrinth/labrinth`）。
- 路由定义位置：`apps/labrinth/src/routes/v2/` 和 `apps/labrinth/src/routes/v3/`。每个端点的路由注册（`.route(path, handler)`）+ 处理函数 + 入参 struct（如 `VersionListFilters`）。
- 对上表每个端点，记录 labrinth 实际的：路径、HTTP 方法、query 参数名集合、参数值期望格式、返回结构。
- **关键**：留意 v2 端点的处理函数是否内部转发 v3（如 `v2::versions` 调 `v3::versions::version_list_internal`），记录转发时的参数包装逻辑。

### ⏳ 阶段 2：逐端点比对参数

- 对照 `IModrinthClient` 的 Refit 签名 vs 阶段 1 的源码真相。
- 检查项（每个 query 参数）：
  - 参数名是否匹配 labrinth 的 query key（snake_case 对齐，`[AliasAs]` 是否齐全）。
  - 参数值格式是否匹配（JSON 数组 `["x"]` vs JSON 对象 `{"k":[...]}` vs 裸字符串）。
  - `ArrayParameterConstructor` 手拼的 JSON 是否符合该端点的期望。
- 标记每个端点：✅ 一致 / ❌ 不一致（列具体差异）。

### ⏳ 阶段 3：实测验证行为

- 对每个带过滤/分页参数的端点，用真实 project/version 发请求验证参数生效。可用 `CurseForgeHelper.API_KEY` 同款方式拿 Modrinth（Modrinth 公开接口无需 key，带 User-Agent 即可）。
- 重点场景：
  - **分页 + 过滤组合**：确认"先过滤后分页"（`limit=N` 返回过滤后的 N 个，不是过滤前的前 N 个）。version 列表、search 都要测。
  - **多值参数**：数组/对象格式是否被正确解析。
  - **参数被忽略的信号**：对比"带参数 vs 不带参数"的返回，数量/内容应有差异；若无差异 = 参数失效。
- 推荐测试样本：`sodium`（mod，版本多，199 个，便于验证过滤）、一个 fabric modpack（验证 loader 语义）、一个 resourcepack（验证非 mod 资源）。

### ⏳ 阶段 4：修复 + 回归

- 修复阶段 2/3 发现的不一致（参数名、值格式、行为）。
- 修复时遵守：API 已正确过滤的，客户端不重复过滤；API 不支持的参数，不传（避免误导）。
- 可选：给关键端点加集成测试守护（网络依赖，用 Mock 或标记 ignore）。

---

## 5. 已知风险（审核时重点关注）

1. **query 参数名 snake_case 对齐** —— Refit 默认用 C# 参数名，v3 多为 snake_case，必须 `[AliasAs]`。已确认 `game_versions` 不是 v3 参数（走 `loader_fields`）。逐个核对所有 `[AliasAs]` 是否齐全、是否多余。
2. **v2/v3 端点混用** —— #1/#3/#4 是 v2。v2 转发 v3 时参数会包装（`game_versions`→`loader_fields`、`loaders` 追加 `mrpack`）。需确认每个 v2 端点的参数语义，以及是否仍该留 v2（v2 可能比 v3 更"宽容"或更"严格"）。`SearchAsync` 的 `facets` 格式 v2/v3 是否一致尤其要查。
3. **分页 + 过滤顺序** —— version 列表的"先分页后过滤"已修，但其他带 `limit/offset` 的端点（search、multiple versions/projects）需逐个确认无类似问题。
4. **参数值格式** —— `loaders` 是数组 `["fabric"]`，`loader_fields` 是对象 `{"game_versions":[...]}`，`facets` 是嵌套数组 `[["categories:fabric"]]`，`ids` 是数组。每个端点的期望格式不同，`ArrayParameterConstructor` 只能造数组，对象/嵌套要别的构造器（参考 `ModrinthRepository.BuildLoaderFields`）。
5. **手拼 JSON 的健壮性** —— `ArrayParameterConstructor` / `BuildLoaderFields` 不转义特殊字符。若传入值含 `"` / `\` 会破坏 JSON。审核时确认所有传入值是否可能含特殊字符（project slug / version number 一般不会，但要确认）。
6. **返回字段映射** —— labrinth 返回 snake_case JSON，靠 `SystemTextJsonContentSerializer` + `PropertyNamingPolicy=SnakeCaseLower` 反序列化到 `Models/ModrinthApi/*.cs`。需确认模型字段齐全且与 labrinth 响应对齐（漏字段 = 静默丢数据）。

---

## 6. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 11 个端点全部比对完 | 每个端点有 ✅/❌ 结论 + 差异说明 |
| 2 | 所有 query 参数实测 | 带 vs 不带参数，返回有预期差异（证明生效）|
| 3 | 分页端点（search/version list） | `limit=N` 返回过滤后的 N 个，非过滤前前 N 个 |
| 4 | v2 端点（#1/#3/#4） | 有明确去留决策（保留/迁 v3）+ 理由 |
| 5 | 模型字段（`Models/ModrinthApi/*`）| 与 labrinth 响应对齐，无静默丢字段 |
| 6 | 修复项 | 改动遵循"API 已过滤则客户端不重复；不支持的参数不传"|

---

## 7. 备选方案备案

### 7.1 验证策略

| 备选 | 做法 | 取舍 |
|------|------|------|
| **A. 源码 + 实测双验证**（采纳）| 既看 labrinth 源码理解机制，又发请求确认生产行为 | 最稳。源码与生产部署有时间差（分页 bug 的修复就是例子），双验证才能兜住 |
| B. 只看 OpenAPI/mintlify 文档 | 查 Modrinth 官方文档 | 文档曾与实现不一致（v2 文档至今挂 `game_versions` 误导了实现），单独依赖不可靠 |
| C. 只发实测请求（黑盒） | 不看源码，纯测 | 能发现"参数无效"但说不清"为什么"，也无法预判服务端改动后的影响 |
| D. 写集成测试套件守护 | 审核同时把每端点行为写成测试 | 最稳但工作量最大，依赖网络/Mock。作为阶段 4 的可选项，不强求 |

### 7.2 query 参数 naming 全局化

当前每个 query 参数靠 `[AliasAs]` 对齐 snake_case，漏一个就失效。备选：实现自定义 `IUrlParameterKeyFormatter`（snake_case）挂到 `RefitSettings.UrlParameterKeyFormatter`，移除所有 `[AliasAs]`。

| 取舍 | 说明 |
|------|------|
| 优点 | 一处配置，所有 query 参数自动 snake_case；新增参数不用记着加 AliasAs |
| 缺点 | Refit 无内置 snake_case formatter，要自己写；作用到 client 所有 query key，需确认没有"故意 camelCase"的参数 |
| 决策 | **不在本计划做**。属于 client 架构改动，独立任务。本计划先靠 `[AliasAs]` 兜底，把"漏加"问题在审核中逐个发现并修复 |

---

## 8. 改动面（预估）

审核完成后的修复可能涉及：

- `submodules/Trident.Net/src/TridentCore.Core/Clients/IModrinthClient.cs` — 参数名/格式/[AliasAs] 修正，可能的 v2→v3 端点替换
- `submodules/Trident.Net/src/TridentCore.Core/Repositories/ModrinthRepository.cs` — 调用点适配（参数构造方式）
- `submodules/Trident.Net/src/TridentCore.Core/Models/ModrinthApi/*.cs` — 模型字段补齐/修正
- 可能新增 query 构造 helper（类似已有的 `BuildLoaderFields`）

Polymerium 主仓侧：子模块 bump 指针（审核修复合入 Trident.Net main 后）。

---

## 9. 已有的相关代码引用

- 端点定义：`submodules/Trident.Net/src/TridentCore.Core/Clients/IModrinthClient.cs`
- 调用方：`submodules/Trident.Net/src/TridentCore.Core/Repositories/ModrinthRepository.cs`（`SearchAsync`/`ResolveAsync`/`ResolveBatchAsync`/`InspectAsync`/`IdentifyAsync`/`QueryAsync`/`QueryBatchAsync`）
- query 构造 helper：`ModrinthRepository.ArrayParameterConstructor`（数组）、`ModrinthRepository.BuildLoaderFields`（对象，2026-07 新增）
- client 注册 + naming：`submodules/Trident.Net/src/TridentCore.Core/Services/RepositoryAgent.cs`（`BuildRepositories`，Modrinth 配 `SnakeCaseLower`，但只影响 JSON 不影响 query）
- labrinth 源码：`github.com/modrinth/code`，`apps/labrinth/src/routes/{v2,v3}/`

---

## 审核记录

（审核进行时在此追加每个端点的结论。格式建议：`### 端点名 — ✅/❌ — 结论 + 证据`）
