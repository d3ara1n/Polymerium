# Modrinth Client 迁移到下一代 API

> 制定日期：2026-07-05
> 定位：把 `IModrinthClient` 从 v2/v3 混用统一迁到 Modrinth 下一代 API（v3 稳定版或 v4），消除混用技术债。
> 当前状态：草案

---

## 1. 背景与动机

2026-07 完成了一次 Modrinth client 全端点审核（原 `MODRINT-API-AUDIT`，已归档），结论是 11 个端点路由全部正确，并修复了 4 处参数/注释问题。审核同时暴露出一个未解决的结构性问题：**client 混用 v2 和 v3 端点**。

### 混用现状

11 个端点里 3 个走 v2、8 个走 v3：

| v2 端点 | v3 端点 |
|---------|---------|
| `GetGameVersionsAsync` `/v2/tag/game_version` | `GetLoadersAsync` `/v3/tag/loader` |
| `GetProjectTypesAsync` `/v2/tag/project_type` | `GetProjectAsync` `/v3/project/{id}` |
| `SearchAsync` `/v2/search` | `GetMultipleProjectsAsync` `/v3/projects` |
| | `GetVersionAsync` `/v3/version/{id}` |
| | `GetMultipleVersionsAsync` `/v3/versions` |
| | `GetTeamMembersAsync` `/v3/team/{id}/members` |
| | `GetProjectVersionsAsync` `/v3/project/{id}/version` |
| | `GetVersionFromHashAsync` `/v3/version_file/{hash}` |

### Modrinth 自己的 v2/v3 定位矛盾

| 来源 | 说法 |
|------|------|
| `api_v2_description.md` | v2 是「current, official API」，承诺 LTS |
| `api_v3_description.md` | v3 是「experimental」，「will be deprecated in the future」 |
| 代码现实 | v2 所有核心路由**内部转发 v3**，v3 才是实现引擎；代码注释又标 `WILL BE REMOVED V3!` 要废弃旧参数 |

文档推 v2、代码推 v3、v1 已死（410 Gone）。**v3 的真实路线图不明**——可能稳定成正式 v3，也可能跳到 v4。在路线图明确前，盲目迁移到当前 v3 有风险：v3 标注 experimental、响应结构未稳定，迁了可能又因 breaking 改动返工。

### 当前已修（2026-07 审核）

迁移时这些修复的**语义**应保留（不一定保留实现）：

| 项 | 内容 |
|----|------|
| P1 | `GetVersionFromHashAsync` 的 `algorithm` 固定 sha1（因唯一调用点 `IdentifyAsync` 用 SHA1），加 NOTE 注释 |
| P2 | `GetProjectVersionsAsync` 的 `loaders` 参数必须 JSON 数组，加 WARNING 注释 |
| P3 | `GetProjectVersionsAsync` 加 `limit`/`offset` 形参，Resolve 场景传 `limit:1` |
| P4 | `BuildFacets` 用新 key（`game_versions`/`project_types`），v2/v3 都兼容 |

---

## 2. 目标 / 非目标 / 不做的事

### 目标

- 等 Modrinth 下一代 API 路线图明确后，把 `IModrinthClient` 的 3 个 v2 端点统一迁到下一代，消除混用。
- 迁移包含模型重写（响应结构对齐）与 query naming 全局化（子项）。

### 非目标

- 不重构 client 架构（不换 HTTP 库、不引入新抽象层、不改 `RepositoryAgent` 的 `RestService.For` 注册方式）。
- 不实现 labrinth 未稳定/未上线的端点。

### 不做的事（本草案不做）

- **不调研 v3 路线图** —— 留作正式施工时的阶段 1。路线图不明时细化施工步骤没有意义。
- **不细化改动** —— 同上。本计划只记录目的、范围、已知关键依据。

---

## 3. 待迁移项（草案级，路线图明确后细化）

### 3.1 端点迁移

3 个 v2 端点要迁。其中两个 tag 端点在 v3 是否有等价物存疑：

| 端点 | 疑点 |
|------|------|
| `GetGameVersionsAsync` | v3 实测 `/v3/tag/game_version` → 404。下一代可能走 loader/loader_fields 的 enum 端点，需调研 |
| `GetProjectTypesAsync` | 同上，需确认下一代有无独立 project_type tag 端点 |
| `SearchAsync` | v3 有 `/v3/search`，但响应结构大改（见 3.2）|

### 3.2 模型重写

`Models/ModrinthApi/*.cs` 要按下一代响应结构对齐。审核中已确认的 **v3 search 响应结构差异**（迁移时作为已知工作量）：

| 字段 | v2 | v3 |
|------|----|----|
| 标题 | `title` | `name` |
| 描述 | `description` | `summary` |
| 类型 | `project_type`（string） | `project_types`（array） |
| 分页 | `limit` / `offset` | `page` / `hits_per_page` |
| side | `"required"`（string） | `["required"]`（array） |

其他端点（project/version/team）的 v3 响应差异需迁移时逐一核对——本次审核未发现丢字段，但未逐字段深审（原审核验收 #5 并入此处）。

### 3.3 query naming 全局化（子项）

当前每个 query 参数靠 `[AliasAs]` 对齐 snake_case，漏一个就静默失效（正是 2026-07 审核 `game_versions` 踩坑的根源）。备选做法：

- 实现自定义 `IUrlParameterKeyFormatter`（snake_case）挂到 `RefitSettings.UrlParameterKeyFormatter`，移除所有 `[AliasAs]`。
- **影响范围**：`RefitSettings` 在 `RepositoryAgent.BuildRepositories` 里被所有 client（CurseForge / Prism / Mojang）共享，snake_case formatter 会作用到非 Modrinth client。迁移时需评估是否有 client 故意用 camelCase 或其他命名，避免误伤。

---

## 4. 已知运行时坑（迁移后需重新验证）

这些是 Modrinth API 的设计坑，迁移到下一代后是否仍存在需要重新测：

| 坑 | 现状 |
|----|------|
| `loaders` 参数只认 JSON 数组，裸字符串/逗号分隔静默返回 0 结果 | v3 确认 |
| `ids` 参数单值裸字符串 → 400（必须数组或逗号分隔） | v3 确认 |
| `loader_fields` / `loaders` JSON 解析失败静默回退空值（不报 400） | v3 确认 |
| `limit` 默认 `usize::MAX`（全量返回） | v3 确认 |

---

## 5. 改动面（粗略）

- `submodules/Trident.Net/src/TridentCore.Core/Clients/IModrinthClient.cs` — 端点路由 + 参数
- `submodules/Trident.Net/src/TridentCore.Core/Models/ModrinthApi/*.cs` — 模型重写
- `submodules/Trident.Net/src/TridentCore.Core/Repositories/ModrinthRepository.cs` — 调用点适配
- `submodules/Trident.Net/src/TridentCore.Core/Utilities/ModrinthHelper.cs` — 映射逻辑适配
- `submodules/Trident.Net/src/TridentCore.Core/Services/RepositoryAgent.cs` — naming 全局化（若做子项）

Polymerium 主仓侧：子模块 bump 指针。

---

## 6. 备选方案备案

### 6.1 迁移时机

| 备选 | 取舍 |
|------|------|
| **等路线图明确后再迁**（采纳）| 避免「迁了又因 breaking 返工」。当前 v2/v3 混用能 work，技术债但不阻塞 |
| 现在迁当前 v3 | 否决：v3 标 experimental、SearchHit 模型重写工作量大、响应结构未稳定 |
| 等 v4 | 若 Modrinth 跳过 v3 稳定直接 v4，则本计划的「下一代」即 v4 |

### 6.2 naming 全局化范围

| 备选 | 取舍 |
|------|------|
| 全局（所有 client 共享 snake_case formatter） | 一处配置，新增参数不用记 AliasAs；但需确认无 client 故意用其他命名 |
| 仅 Modrinth client（独立 RefitSettings） | 隔离副作用，但 `RepositoryAgent` 现在是共享 settings，要拆 |

决策待迁移时定，取决于评估其他 client 的 naming 需求。

---

## 7. 启动条件

正式施工前必须先完成（作为阶段 1）：

1. **调研 Modrinth API 路线图**：v3 会稳定吗？何时？还是跳 v4？跟进 modrinth/code 仓库的 version policy / 公告。
2. 路线图明确后，把本计划从「草案」升「蓝本」，细化施工步骤、逐端点核对响应结构、补模型字段对齐。
