# Repository Cache Coordination With FusionCache

> 制定日期：2026-06-27
> 定位：用 FusionCache 接管 Trident 仓库数据的缓存协调，在 Polymerium 中保留 NeoSmart SQLite 作为持久化 L2，让 RepositoryAgent 获得 L1+L2、击穿保护与失效兜底能力，同时避免业务代码直接维护序列化和缓存命中流程。
> 当前状态：✅ 已实施

---

## 计划

### 背景与动机

Polymerium 之前通过 NeoSmart.Caching.Sqlite.AspNetCore 注册 SQLite 持久化缓存，Trident 的 RepositoryAgent 直接消费 IDistributedCache，并在自身内部手写缓存读取、MessagePack 序列化、反序列化、写回和错误处理。这个结构能提供跨启动持久化缓存，但有几个问题：

- NeoSmart 长期缺少维护，仍传递旧版 SQLitePCLRaw 原生库并触发漏洞告警。
- RepositoryAgent 直接面对字节缓存，缓存协调逻辑散落在仓库访问代码里。
- 同一个 key 并发击穿时缺少统一的 factory 合并机制。
- 持久化缓存读写异常、慢查询和过期旧值兜底都需要业务层自己处理。

曾考虑在 Trident.Net 内自建 SQLite IDistributedCache 替换 NeoSmart，但完整实现持久化 KV、TTL、损坏恢复、并发控制和迁移策略的成本明显高于当前收益。BLite.Caching 也曾作为替代候选实测，但效果不如 NeoSmart，不适合作为落地方案。

最终选择保留 NeoSmart 作为现有持久化 L2，同时引入 FusionCache 作为 Trident 仓库数据的缓存协调层。

### 目标

- RepositoryAgent 不再手写 byte[] 序列化缓存流程，而是表达仓库数据的缓存语义。
- FusionCache 作为仓库缓存协调层，提供进程内 L1、持久化 L2、击穿保护、fail-safe 和 timeout 能力。
- Polymerium 继续使用 NeoSmart SQLite 作为 L2，保留跨启动缓存能力。
- Trident CLI 也能独立运行，使用 FusionCache 纯 L1，保持原本非持久化缓存语义。
- DataService 只缓存 UI hot data 和应用层加工结果，Package / Project / Description / Changelog / Status 直接委托 Trident 仓库缓存层。
- 显式初始化 SQLitePCLRaw provider，避免 FreeSql 继续依赖 NeoSmart 的隐式 SQLite 初始化副作用。

### 非目标

- 不在本次替换 NeoSmart。
- 不消除 NeoSmart 传递的 SQLitePCLRaw 旧版漏洞告警。
- 不复用旧缓存文件内容。缓存是可丢弃数据，旧格式失效对用户无感。
- 不把 DataService 的图片、loader、featured、截断版本列表等 UI hot data 缓存迁入 Trident。
- 不新增通用缓存抽象。当前明确接受 TridentCore.Core 依赖 FusionCache 生态。

### 关键决策

#### FusionCache 作为 Trident 仓库缓存协调层

RepositoryAgent 直接依赖 IFusionCache，而不是继续依赖 IDistributedCache。这样 Trident 的仓库访问代码直接获得 FusionCache 的 L1/L2 联动、factory 合并、fail-safe 和 timeout 能力。

这个选择意味着 TridentCore.Core 绑定 FusionCache 生态。该绑定是有意为之：FusionCache 是缓存协调层，而不是具体磁盘存储，具体 L2 仍由宿主选择。

#### Polymerium 使用 NeoSmart 作为 FusionCache L2

Polymerium 继续注册 NeoSmart SQLite cache，并让 FusionCache 使用已注册的 IDistributedCache 作为 L2。这样运行链路为：

```text
RepositoryAgent
  -> FusionCache L1 memory
  -> NeoSmart SQLite L2
  -> repository factory
```

L1 命中时直接返回对象；L1 miss 时读取 L2；L2 miss 时执行远端 factory；factory 成功后写回 L1 和 L2。

#### Trident CLI 使用 FusionCache 纯 L1

CLI 原先注册 DistributedMemoryCache，本身没有持久化 L2。为保持 CLI 独立可用且避免无意义的 serializer 配置，CLI 注册 FusionCache 但不挂 distributed cache。

#### MessagePack 由宿主作为 FusionCache L2 serializer 使用

RepositoryAgent 不再直接调用 MessagePackSerializer。Polymerium 通过 FusionCache 的 Neuecc MessagePack serializer 将对象写入 L2。显式引用 MessagePack 最新稳定版本，避免 serializer 包传递旧版本导致漏洞告警。

#### SQLitePCLRaw 初始化属于应用级 native dependency 初始化

Polymerium 显式引用 SQLitePCLRaw.bundle_e_sqlite3，并在应用早期调用 Batteries_V2.Init 和 FreezeProvider。这样 FreeSql 不再依赖 NeoSmart 的初始化副作用，NeoSmart 后续初始化也不能覆盖应用选择的 provider。

### DataService 职责边界

DataService 不是仓库数据的通用 L0。当前职责分为两类：

- Package / Project / Description / Changelog / Status 直接委托 RepositoryAgent，由 Trident 仓库缓存层管理。
- Bitmap、图标文件、loader 元数据、Minecraft 版本、Mojang news、featured modpacks、截断版本列表等 UI hot data 或应用层加工结果仍由 DataService 内存缓存管理。

这个边界保留了 DataService 对 UI 热数据的价值，同时避免仓库数据在 Polymerium 和 Trident 两侧重复维护缓存策略。

### 验收标准

- Polymerium 启动后 RepositoryAgent 能解析、查询、读取描述和 changelog。
- 重复访问同一仓库数据时，进程内命中 FusionCache L1，不需要反复访问 NeoSmart L2。
- 应用重启后，NeoSmart L2 仍能为 FusionCache 提供跨启动缓存。
- 同一 key 并发 miss 时，FusionCache 合并 factory 调用。
- 远端仓库短时失败时，启用 fail-safe 的缓存项可按 FusionCache 策略使用旧值兜底。
- ResolveBatchAsync 和 QueryBatchAsync 仍保留批量远端 API：先读取缓存，miss 后按 repository 分组批量请求，再写回缓存。
- Trident CLI 能独立构建和运行，不要求宿主提供 NeoSmart。
- FreeSql 在没有 NeoSmart 初始化副作用时仍能正常初始化 SQLite。

### 风险与取舍

| 风险 | 取舍 |
|------|------|
| TridentCore.Core 绑定 FusionCache | 接受该绑定，换取缓存协调能力和更清晰的仓库缓存语义 |
| NeoSmart 旧依赖仍存在 | 本次不替换 L2，先改善业务缓存层；旧依赖问题以后单独处理 |
| L2 wire format 改变 | 缓存可丢弃，旧缓存失效对用户无感 |
| DataService 和 RepositoryAgent 都有内存缓存 | 两者缓存不同数据：RepositoryAgent 缓存仓库数据，DataService 缓存 UI hot data 和加工结果 |
| FusionCache serializer 传递 MessagePack | 显式引用最新稳定 MessagePack，避免旧版本漏洞告警 |

### 备选方案备案

#### 自建 TridentCore.Caching

可彻底替换 NeoSmart，但实现成本高，需要自行处理持久化 KV、TTL、并发、损坏恢复、schema、迁移和测试。当前收益不足以支撑完整自建库。

#### BLite.Caching 替换 NeoSmart

包活跃且不依赖 SQLite，但实测效果不如 NeoSmart，且无法直接满足当前性能和稳定性预期。已放弃。

#### FusionCache 纯 L1

可立即移除 NeoSmart 和旧 SQLitePCLRaw 告警，但会丢失跨启动缓存。当前选择保留 NeoSmart L2。

#### 继续维持 RepositoryAgent 手写 IDistributedCache

改动最小，但无法获得 L1 对象缓存、factory 合并、fail-safe 和统一缓存策略。已放弃。

---

## 方案

✅ 已完成：提交回退基线 `b32369ca fix(cache): initialize SQLite provider explicitly`，保留 NeoSmart，同时显式初始化并冻结 SQLitePCLRaw provider。

✅ 已完成：Polymerium 引入 FusionCache、FusionCache Neuecc MessagePack serializer 和显式 MessagePack 版本，注册 FusionCache 默认策略，并连接已注册 NeoSmart IDistributedCache 作为 L2。

✅ 已完成：RepositoryAgent 改为依赖 IFusionCache，单项缓存使用 GetOrSetAsync，缓存读取使用 TryGetAsync，写回使用 SetAsync，删除手写 MessagePack 序列化和 IDistributedCache 直接访问。

✅ 已完成：ResolveBatchAsync 和 QueryBatchAsync 保留原有批量远端调用结构，只把缓存读取和写回切换到 FusionCache。

✅ 已完成：Trident CLI 注册 FusionCache 纯 L1，使 Trident.Net 独立使用时不需要 Polymerium 的 NeoSmart L2。

✅ 已完成：DataService 注释更新为当前职责边界，明确仓库数据交给 Trident 仓库缓存层，DataService 只缓存 UI hot data 和应用层加工数据。

验证命令：

```text
dotnet build "src/Polymerium.Avalonia/Polymerium.Avalonia.csproj"
```

当前构建通过。剩余 NuGet warning 来自保留 NeoSmart 后传递的 SQLitePCLRaw.lib.e_sqlite3 2.1.10，属于已知未解决风险。
