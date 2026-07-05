# Modrinth Client API 审核记录

> 审核日期：2026-07-05
> 方法：源码（labrinth `modrinth/code` @ `v0.15.7`）+ 生产 API 实测（api.modrinth.com）双验证
> 审核对象：`submodules/Trident.Net/src/TridentCore.Core/Clients/IModrinthClient.cs` 的 11 个端点

配套计划：`plans/MODRINT-API-AUDIT.md`

---

## 1. 端点逐个结论

| # | 端点 | 路由 | 参数 | 源码 | 实测 | 结论 |
|---|------|------|------|------|------|------|
| 1 | `GET /v2/tag/game_version` | ✅ | client 无参（v2 实际支持 `type`/`major`，转发 v3 loader_fields） | ✅ | ✅ 900 条 | **一致**。client 取全量无需额外参数 |
| 2 | `GET /v3/tag/loader` | ✅ | 无 | ✅ | ✅ 30 条 | **一致** |
| 3 | `GET /v2/tag/project_type` | ✅ | 无（转发 v3） | ✅ | ✅ 7 种 | **一致** |
| 4 | `GET /v2/search` | ✅ | query/facets/index/offset/limit | ✅ | ✅ 5 种 index 全生效；facets 外 AND 内 OR | **⚠️ facets key 用旧风格**（见 P4）|
| 5 | `GET /v3/project/{id}` | ✅ | path | ✅ | ✅ | **一致** |
| 6 | `GET /v3/projects` | ✅ | ids（JSON 数组） | ✅ | ✅ 裸字符串 400，数组/逗号分隔 OK | **一致**。client 用 `JsonSerializer.Serialize` 生成数组，正确 |
| 7 | `GET /v3/version/{id}` | ✅ | path | ✅ | ✅ | **一致** |
| 8 | `GET /v3/versions` | ✅ | ids（JSON 数组）+ include_changelog | ✅ | ✅ 同 #6 | **一致** |
| 9 | `GET /v3/team/{id}/members` | ✅ | path | ✅ | ✅ | **一致** |
| 10 | `GET /v3/project/{id}/version` | ✅ | version_type/loaders/loader_fields/include_changelog | ✅ filter→sort→paginate | ✅ 分页过滤顺序已修；loader_fields 多 key AND 生效 | **⚠️ 缺 limit/offset 形参**（见 P3）；loaders 格式严格（见 P2）|
| 11 | `GET /v3/version_file/{hash}` | ✅ | algorithm | ✅ 自动检测（按 hash 长度） | ✅ sha1/sha512 都 OK；**sha512+algorithm=sha1 → 404** | **🔴 algorithm 默认值陷阱**（见 P1）|

**路由层结论**：11/11 端点路由路径与 HTTP 方法全部与 labrinth 源码一致，无幽灵端点、无路径偏差。

---

## 2. 源码 vs 文档 vs 通常做法 — 偏差表

| 端点 | 维度 | 文档/通常做法 | labrinth 实际 | 影响 |
|------|------|--------------|--------------|------|
| #10 `project/{id}/version` | loaders 解析失败 | 通常返回 400 | `unwrap_or_default()` 静默回退空 Vec，返回未过滤全量 | 调用方传错格式不报错，以为过滤生效实际没生效 |
| #10 | loader_fields 解析失败 | 通常返回 400 | 静默回退空 HashMap，返回未过滤全量 | 同上 |
| #10 | `loaders` 值格式 | 多数 API 接受 `a,b` 或重复参数 | **只接受 JSON 数组** `["fabric"]`；裸字符串/逗号分隔 → 0 结果（静默）| 与 #6/#8 的 `ids`（接受逗号分隔）**格式不统一** |
| #6/#8 `projects`/`versions` | `ids` 单值 | 多数 API 接受裸字符串 | 裸字符串 → 400；必须 `["id"]` 或 `id1,id2` | 单 ID 也得包数组 |
| #10 | `limit` 默认值 | 通常 10–50 | `usize::MAX`（全量返回）| 不传 limit 一次拉全部，sodium=199 条全返回 |
| #11 `version_file/{hash}` | algorithm 默认 | — | 不传时按 hash 长度自动检测（≥128→sha512，否则 sha1）| API 设计合理，但 client 强制传 `"sha1"` 破坏了自动检测 |
| #4 `search` (v2) | facets key | v3 文档/mintlify 用新 key（`game_versions`）| v2 handler 把旧 key（`versions`/`project_type`/`title`）静默改写为新 key；直接传新 key 也认 | v2/v3 facets key 不通用；旧 key 在 SearchQuery 里标注 `WILL BE REMOVED V3!` |
| v2/v3 定位 | 版本政策 | v2 文档说 "LTS、官方推荐"；v3 文档说 "experimental、将会废弃" | **代码里 v2 全部转发 v3**，v3 才是实现引擎；代码注释又标 `WILL BE REMOVED V3!` 要废弃旧参数 | 文档与代码自相矛盾；client 混用 v2/v3 的策略需关注 v3 是否真会被砍 |

---

## 3. 运行时坑汇总（实测复现）

| # | 端点 | 坑 | 复现 | 严重度 |
|---|------|----|------|--------|
| 1 | #10 | `loaders=fabric`（裸字符串）→ 0 结果，不报错 | `?loaders=fabric` vs `?loaders=["fabric"]` | 🔴 高（client 现状用数组，不触发）|
| 2 | #10 | `loaders=fabric,quilt`（逗号分隔）→ 0 结果 | 同上 | 🔴 高（同上）|
| 3 | #11 | sha512 hash + `algorithm=sha1` → 404 | sha512 hash 配默认 algorithm | 🔴 高（client 现状只算 sha1，暂不触发，但是地雷）|
| 4 | #6/#8 | 单个裸 ID `ids=AANobbMI` → 400 | 传纯字符串 | ⚠️ 中（client 用数组，不触发）|
| 5 | #10 | modpack 版本的 `loaders` 字段是 `["mrpack"]` 而非 `["fabric"]`，用 fabric 过滤 modpack 无效 | modpack 上 `loader_fields={"loaders":["fabric"]}` | ⚠️ 中（client 的 `GetVersionLoaderFilter` 对 modpack 返回 loader 名，需确认是否正确）|
| 6 | #1 | `/v3/tag/game_version` 不存在（404），只有 v2 有 | — | ⚠️ 低（client 已用 v2）|

---

## 4. 分页 + 过滤顺序（重点验证项）

**结论：✅ 生产 API 已是「先过滤后分页」，传闻的「先分页后过滤」bug 已修复。**

实测证据（sodium，199 版本）：
- `limit=5` 无过滤 → 5 条，末版本 `JjCVwmVA`
- `limit=5` + `game_versions=["1.21.4"]` → 5 条，末版本 `GUEd3mz0`（≠ 无过滤的末版本，证明不是「取前 5 再筛」）
- 无 limit + `game_versions=["1.21.4"]` → 16 条（limit=5 取的是这 16 条里的前 5）

源码确认：`filter → sort(desc by date_published) → skip(offset).take(limit)`，注释明确 `// Sort before applying limit/offset so that limit=N returns the N newest versions`。

---

## 5. 待修复项（建议）

### P1 🔴 `GetVersionFromHashAsync` — algorithm 默认值

**现状**
```csharp
[Get("/v3/version_file/{hash}")]
Task<VersionInfo> GetVersionFromHashAsync(string hash, [Query] string algorithm = "sha1");
```

**问题**：Refit 会把默认值 `"sha1"` 当作真实 query 发送，覆盖 API 的自动检测。sha512 hash 场景 → 404。

**当前风险**：低。`IdentifyAsync` 用 `SHA1.HashData` 只算 sha1，所以暂不触发。但只要将来支持 sha512 或外部传入 sha512 hash 就炸。

**建议修法**：改签名为 `string? algorithm = null`，配合 Refit 的 `[Query]` 在 null 时不发送该参数（需验证 Refit 行为；若 Refit 仍发送空串，改用两个重载或自定义 query 构造）。

### P2 🔴 `loaders` 参数格式严格性（静默失败，文档标注）

**现状**：client 用 `ArrayParameterConstructor` 生成 `["fabric"]`，**格式正确，不触发**。

**问题**：Modrinth API 设计坑——`loaders` 只认 JSON 数组，裸字符串/逗号分隔静默返回 0 结果，与 `ids` 参数（接受逗号分隔）格式不统一。

**建议修法**：不改代码，在 `IModrinthClient.GetProjectVersionsAsync` 的 `loaders` 参数加 `// WARNING:` 注释，标注「必须是 JSON 数组字符串，裸字符串/逗号分隔会静默返回空」。

### P3 ⚠️ `GetProjectVersionsAsync` 缺 `limit`/`offset` 形参

**现状**：client 签名无 limit/offset，每次拉项目全量版本。

**问题**：API 支持 limit/offset；sodium 199 版本每次全拉。`ResolveAsync` 拿到全量后 `OrderByDescending(DatePublished).FirstOrDefault()`——逻辑对，但多拉了 198 条无用数据。

**建议修法**：给 `GetProjectVersionsAsync` 加 `uint? limit = null` / `uint? offset = null`（带 `[AliasAs]`），`ResolveAsync`/`ResolveBatchAsync` 在只取最新时传 `limit: 1`。`InspectAsync`（要全量本地分页）保持不传。

**取舍**：是优化非 bug。改 client 签名 + 3 个调用点。若想保持「最小改动」可跳过。

### P4 ⚠️ `BuildFacets` 用 v2 旧 key

**现状**（`ModrinthHelper.BuildFacets`）：
```csharp
facets.Add(new("versions", gameVersion));       // v2 旧 key
facets.Add(new("project_type", projectType));   // v2 旧 key
```

**问题**：靠 v2 search handler 的 key 改写（`versions`→`game_versions`）兜底。源码里这些旧 key 标注 `// TODO: Deprecated values below. WILL BE REMOVED V3!`。且 v3 search 不做改写，旧 key 直接传 v3 会失效。

**建议修法**：改用新 key `game_versions` / `project_types`。v2 handler 只改写「精确匹配旧 key」的，新 key 不触发改写但后端 typesense 字段名就是新 key，所以 v2/v3 都兼容。

**风险**：低。facets 是字符串拼接，改 key 名即可。但需实测验证 v2 直接传新 key 确实生效（源码逻辑支持，但未实测）。

---

## 6. 不修的（记录在案）

- **v2/v3 混用现状**：v2 tag 端点转发 v3、v2 search 不走 v3 转发，混用风险低。迁移是独立决策。
- **query naming 全局化**（`IUrlParameterKeyFormatter`）：作用于共享 `RefitSettings`，会影响 CurseForge 等其他 client，独立任务。
- **模型字段对齐**（`Models/ModrinthApi/*`）：本次实测未发现丢字段，但未逐字段深审响应 vs 模型。如需可另起字段级审计。
- **手拼 JSON 转义**：`JsonSerializer.Serialize` 默认转义 `"`/`\`，无风险（用户已确认）。

---

## 7. 生产部署版本

- labrinth 最新 tag：`v0.15.7`（main HEAD）
- 关键修复（version 列表 filter→sort→paginate）已在源码确认
- 生产是否已部署 `v0.15.7`：**无法从仓库确认**（需 Modrinth 运营方信息）。但实测打生产 API 确认分页过滤顺序已正确，说明生产至少包含该修复
