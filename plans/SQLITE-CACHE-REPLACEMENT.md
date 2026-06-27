# 自制 SQLite 持久化缓存库（TridentCore.Caching）

> 制定日期：2026-06-27
> 定位：长期计划。在 `submodules/Trident.Net` 内新增 `TridentCore.Caching` 项目，实现一个机制完善、功能强大的 SQLite 持久化缓存，作为 `IDistributedCache` 提供给 `RepositoryAgent` 等消费方，替换外部依赖 `NeoSmart.Caching.Sqlite.AspNetCore`。
> 当前状态：**未实施**。本文档是未来实施的蓝本，据此编码不需重新调研。

---

## 1. 背景与动机

### 1.1 现状与触发原因

`src/Polymerium.Avalonia/Startup.cs` 当前用 `NeoSmart.Caching.Sqlite.AspNetCore` 9.0.1 注册 SQLite 持久化缓存，缓存文件 `PathDef.Default.PrivateCacheDirectory()/cache.sqlite.db`。唯一消费方是 `submodules/Trident.Net/.../RepositoryAgent`，通过 `IDistributedCache` 注入，缓存 HTTP 仓库数据（modpack 元数据、版本列表）。

Sentry 上两个**启动期崩溃** issue，根因都在 NeoSmart 库的连接初始化零容错：

| Issue | 根因（NeoSmart 源码事实） |
|-------|--------------------------|
| **POLYMERIUM-1C** `table "cache" already exists` | `Connect()` 检测旧库不兼容 → `File.Delete(主db)`，但库自身设了 `PRAGMA journal_mode=WAL`，删主文件后 `-wal`/`-shm` 旁车残留 → 下次 Open SQLite 从 wal 回放出 `cache` 表 → `Initialize()` 用裸 `CREATE TABLE "cache"`（无 `IF NOT EXISTS`）撞"已存在" |
| **POLYMERIUM-22** `file being used by another process` | `File.Delete` 裸调不 catch → 文件被占用（双开残留/杀软/同步盘）→ `IOException` 冒泡到 `Main` → `AppDomain.UnhandledException` → 崩溃 |

单实例闸门已堵住"主动双开"这一类，但堵不住「崩溃残留半写 db」「杀软锁文件」「schema 不一致」。缓存层自身容错才是根治。

### 1.2 为什么自建而不继续用 NeoSmart

- NeoSmart 的连接/建表/恢复流程零容错，每个意外都让应用启动崩溃（见上表）
- 库约 115 star、单人维护、9.0.1 是当前最新，质量与活跃度不足以作为启动关键路径依赖
- Polymerium 的需求**极窄**：带 TTL 的持久化 KV，`IDistributedCache` 的 8 个方法。自实现成本可控，且能把容错与机制做到位

### 1.3 为什么放进 Trident.Net 而非 Polymerium

消费方 `RepositoryAgent` 就在 `TridentCore.Core` 内。缓存是 Trident 仓库抽象的天然配套基建，放 Polymerium 主项目里会让 Trident 反向依赖宿主——方向反了。作为 `TridentCore.Caching`，它服务于 Trident 自己的模块，Polymerium 通过引用 Trident 间接获得能力，依赖方向正确。

---

## 2. 设计原则

| 原则 | 含义 |
|------|------|
| **机制完善** | 连接、建表、并发、过期、清理、损坏恢复每一步都明确且正确，不靠"碰巧能跑" |
| **纯净契约** | 库提供能力、不规定策略。构造失败抛异常，由消费方决定怎么办（no-op 包装 / 崩溃 / 重试） |
| **不兼容旧库** | 全新 schema 设计，不为 NeoSmart 的数据格式妥协。旧 `cache.sqlite.db` 由 Polymerium 首次启动时整体删除重建（缓存数据丢失无感） |
| **不用 WAL** | 见 §3.2。回退 DELETE journal 模式，从机制上消灭"旁车文件残留"这一整类 bug |
| **不内置内存降级** | 不在库内做"失败转内存缓存"。Polymerium 已有 `IMemoryCache`，库再做内存降级等于翻倍内存。失败策略交给消费方 |
| **不追求分布式** | 单机持久化，不考虑多节点一致性 |

---

## 3. 核心机制设计

### 3.1 表结构（全新设计，无兼容包袱）

```sql
CREATE TABLE IF NOT EXISTS cache_entries (
    cache_key      TEXT    PRIMARY KEY,
    cache_value    BLOB    NOT NULL,
    expires_at     INTEGER,          -- 绝对过期（UTC ticks），NULL = 永不过期
    sliding_span   INTEGER,          -- 滑动窗口（ticks），NULL = 不滑动；命中时 expires_at += sliding_span
    created_at     INTEGER NOT NULL,
    updated_at     INTEGER NOT NULL
) WITHOUT ROWID;

CREATE INDEX IF NOT EXISTS ix_cache_entries_expires_at
    ON cache_entries(expires_at) WHERE expires_at IS NOT NULL;

CREATE TABLE IF NOT EXISTS cache_meta (
    meta_key   TEXT PRIMARY KEY,
    meta_value INTEGER NOT NULL
) WITHOUT ROWID;

INSERT OR IGNORE INTO cache_meta(meta_key, meta_value) VALUES ('schema_version', 1);
```

**设计要点（与 NeoSmart 的对比体现"机制完善"）**：

- 列名加 `cache_` 前缀，避开 `key`/`value` 这类 SQL 保留字——NeoSmart 被迫写 `"key"` 加引号是 hack，本设计从根上干净
- `WITHOUT ROWID`：主键是 TEXT，用 rowid 反而多一层间接寻址，去掉更省空间更快
- 部分索引 `WHERE expires_at IS NOT NULL`：永不过期的条目不进过期索引，减小索引体积、加快清理扫描
- `cache_meta.schema_version`：库自身未来升级 schema 时用，由库自己管理（首版为 1），**不读取 NeoSmart 旧库的版本号**
- 全部用 `IF NOT EXISTS` / `INSERT OR IGNORE`：建表/写版本号幂等，即使逻辑有疏漏也不会撞"已存在"

### 3.2 Journal 模式：DELETE，不用 WAL

这是本设计与 NeoSmart 最关键的分野，也是对 POLYMERIUM-1C 的根治。

| 模式 | 文件 | crash safety | 读写并发 | 取舍 |
|------|------|-------------|---------|------|
| **WAL**（NeoSmart 用的） | `.db` + `.db-wal` + `.db-shm` 常驻三件套 | ✅ | ✅ 读不阻塞写 | 旁车文件管理复杂；删主文件不删旁车 = wal 回放 = 1C 的根因 |
| **DELETE**（本设计） | `.db` + 事务期临时 `.db-journal`（结束自动删） | ✅ | ❌ 写锁全库 | 无常驻旁车；缓存场景读多写少，并发劣势可忽略 |

DELETE 模式下，rollback journal 只在写事务期间存在、事务结束即删除，不会常驻。**"残留旁车导致回放出表"这一整类 bug 在 DELETE 模式下从机制上不可能发生。** 代价是写时锁全库，但缓存场景（RepositoryAgent 偶发写、大量读）吞吐远低于 SQLite 单线程上限，可忽略。

明确设置：

```csharp
// Open 之后立即固化（每次都设，幂等）
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "PRAGMA journal_mode = DELETE;";
    cmd.ExecuteNonQuery();
}
```

### 3.3 连接与自愈流程

库的构造期流程。**失败抛异常，不在库内降级**——这是纯净契约的体现：

```
SqliteCache(options):
    EnsureParentDir(options.CachePath)
    conn = new SqliteConnection(connectionString)
    conn.Open()

    if File.Exists(options.CachePath):
        # 不做 NeoSmart 那种脆性"对象数==3"校验
        # 只做最低限度的"能否正常查询"探活
        if not Probe(conn):              # SELECT 1 FROM cache_entries LIMIT 1 能跑
            conn.Dispose()
            SafeDelete(options.CachePath) # 删主 .db（DELETE 模式无旁车，单文件）
            conn = new SqliteConnection(...)
            conn.Open()

    InitializeSchema(conn)               # 全部 IF NOT EXISTS，幂等
    SetPragmas(conn)                     # journal_mode=DELETE
    StartCleanupTimer(conn, options.CleanupInterval)
    _conn = conn
    # 成功返回；任何步骤抛异常都原样向上传播，消费方处理
```

**与 NeoSmart 的容错对比**：

| 场景 | NeoSmart | 本设计 |
|------|----------|--------|
| db 文件被占用，删不掉 | 裸 `File.Delete` 抛 → 崩溃（22） | `SafeDelete` 内部 catch 并包装成明确异常向上抛，**消费方可 catch 后选择 no-op**；库不崩、不擅自降级 |
| schema 不兼容 | `CheckExistingDb` 判断脆（数对象数） | `Probe` 只验证"能不能查"，宽松；不兼容直接重建 |
| 建表撞已存在 | `CREATE TABLE` 裸 SQL → 崩溃（1C） | 全程 `IF NOT EXISTS`，机制上不可能撞 |

### 3.4 `IDistributedCache` 契约实现

8 个方法（同步 × 4 + 异步 × 4）。**操作级容错约定**：每个方法体内 try-catch `SqliteException`，命中后——

- `Get/GetAsync`：记日志，返回 `null`（语义=未命中，消费方走真实路径，不会因缓存单次故障而崩）
- `Set/SetAsync/Refresh/RefreshAsync/Remove/RemoveAsync`：记日志，静默返回（写入失败只影响这一次，不阻塞调用方业务）

> 连接级（§3.3）失败抛异常、由消费方在注册层决定；操作级（本节）失败吞掉返回安全默认值。两层容错各司其职：连接级管"起得来"，操作级管"跑得稳"。

操作 SQL（全部参数化）：

```sql
-- Get（命中且 sliding_span 非空时顺带续期，单事务）
SELECT cache_value, sliding_span FROM cache_entries
WHERE cache_key = @key AND (expires_at IS NULL OR expires_at > @now);

-- 命中后续期：
UPDATE cache_entries SET expires_at = @now + sliding_span, updated_at = @now
WHERE cache_key = @key AND sliding_span IS NOT NULL;

-- Set（INSERT OR REPLACE 覆盖既有键）
INSERT OR REPLACE INTO cache_entries
(cache_key, cache_value, expires_at, sliding_span, created_at, updated_at)
VALUES (@key, @value, @expiry, @sliding, @now, @now);

-- Refresh
UPDATE cache_entries SET expires_at = expires_at + sliding_span, updated_at = @now
WHERE cache_key = @key AND sliding_span IS NOT NULL;

-- Remove
DELETE FROM cache_entries WHERE cache_key = @key;

-- 后台过期清理
DELETE FROM cache_entries WHERE expires_at IS NOT NULL AND expires_at <= @now;
```

---

## 4. 功能增强（"功能强大"的体现）

`IDistributedCache` 是基础契约，本库在此之上提供面向实际使用场景的增强。这些增强通过**独立接口**暴露，消费方按需注入，不污染基础契约：

### 4.1 批量操作 `IBulkableCache`

```csharp
public interface IBulkableCache
{
    IReadOnlyDictionary<string, byte[]> GetMany(IEnumerable<string> keys);
    Task<IReadOnlyDictionary<string, byte[]>> GetManyAsync(IEnumerable<string> keys, CancellationToken token = default);
    void SetMany(IEnumerable<KeyValuePair<string, byte[]>> entries, DistributedCacheEntryOptions options);
    Task SetManyAsync(IEnumerable<KeyValuePair<string, byte[]>> entries, DistributedCacheEntryOptions opts, CancellationToken token = default);
}
```

单事务批量读写，避免 N 次 round-trip。`RepositoryAgent` 拉取一个 modpack 的多版本元数据时受益明显。

### 4.2 命中率统计 `ICacheStatistics`

```csharp
public interface ICacheStatistics
{
    CacheStatistics Snapshot();   // 命中/未命中/总数/估算字节数
}

public sealed record CacheStatistics(long Hits, long Misses, long EntryCount, long EstimatedBytes)
{
    public double HitRate => Hits + Misses == 0 ? 0 : (double)Hits / (Hits + Misses);
}
```

内部用 `Interlocked` 计数，零锁开销。便于 Polymerium 在诊断面板展示缓存健康度，也便于 Sentry 上报降级/低命中事件。

### 4.3 显式清理 `ICacheMaintenance`

```csharp
public interface ICacheMaintenance
{
    int PurgeExpired();             // 立即清理过期，返回清理条数
    int Count { get; }              // 当前条目数
    long PurgeAll();                // 清空全部，返回清理条数（谨慎）
}
```

手动触发清理（GC 功能、设置页"清理缓存"按钮可直接调用），不必等后台定时器。

### 4.4 配置选项

```csharp
public sealed record SqliteCacheOptions
{
    public required string CachePath { get; init; }
    public TimeSpan? CleanupInterval { get; init; } = TimeSpan.FromMinutes(10);
    public bool EnableStatistics { get; init; } = true;
    public int MaxRetryOnLock { get; init; } = 3;        // SQLITE_BUSY 时的重试次数
    public TimeSpan RetryDelayOnLock { get; init; } = TimeSpan.FromMilliseconds(100);
}
```

`MaxRetryOnLock` 处理 DELETE 模式下写锁冲突（`SQLITE_BUSY`）——小幅重试 + 退避，避免偶发并发写直接失败。这是"机制完善"在并发侧的体现。

### 4.5 DI 扩展方法

提供与 NeoSmart 同形态的注册扩展，替换时只改一行：

```csharp
// TridentCore.Caching 内
public static class SqliteCacheServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteCache(
        this IServiceCollection services,
        Action<SqliteCacheOptions> setup)
    {
        services.AddSingleton<SqliteCache>(sp =>
        {
            var opts = new SqliteCacheOptions();
            setup(opts);
            return new SqliteCache(opts, sp.GetRequiredService<ILogger<SqliteCache>>());
        });
        services.AddSingleton<IDistributedCache>(sp => sp.GetRequiredService<SqliteCache>());
        // 增强接口按需暴露，消费方注入 IBulkableCache 等即可
        services.AddScoped<IBulkableCache>(sp => sp.GetRequiredService<SqliteCache>());
        services.AddScoped<ICacheStatistics>(sp => sp.GetRequiredService<SqliteCache>());
        services.AddScoped<ICacheMaintenance>(sp => sp.GetRequiredService<SqliteCache>());
        return services;
    }
}
```

---

## 5. 项目结构

```
submodules/Trident.Net/
└── src/
    └── TridentCore.Caching/                   ← 新增项目
        ├── TridentCore.Caching.csproj
        ├── SqliteCache.cs                     ← 主类：IDistributedCache + 增强接口实现
        ├── SqliteCacheOptions.cs              ← 配置 record
        ├── SqliteCacheServiceCollectionExtensions.cs  ← AddSqliteCache
        ├── IBulkableCache.cs                  ← 批量接口
        ├── ICacheStatistics.cs                ← 统计接口 + CacheStatistics record
        ├── ICacheMaintenance.cs               ← 维护接口
        └── SqliteExceptionExtensions.cs       ← SQLITE_BUSY 判定等辅助
```

### 5.1 csproj 依赖

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <LangVersion>default</LangVersion>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.9" />
        <PackageReference Include="SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.10" />
        <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="10.0.9" />
        <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.9" />
    </ItemGroup>
</Project>
```

- `Microsoft.Data.Sqlite` + `SQLitePCLRaw.bundle_e_sqlite3`：自管理 SQLite 引擎，与 Trident 现有依赖解耦（Trident 内部此前不依赖 SQLite）
- 不依赖 `Microsoft.Extensions.DependencyInjection` 之外的 DI 框架——`AddSqliteCache` 扩展仅依赖 `IServiceCollection`，由消费方（Polymerium）已注册的 DI 容器承接
- 记得在 `submodules/Trident.Net` 根 sln 以及 Polymerium.slnx 里加入此项目

### 5.2 Polymerium 侧适配（no-op fallback）

库本身失败抛异常（纯净契约），但 Polymerium 不能崩（POLYMERIUM-22 的教训）。**这个适配责任在 Polymerium 消费侧，不在库里**：

```csharp
// Startup.cs 注册改造
services.AddSqliteCache(setup =>
{
    setup.CachePath = Path.Combine(PathDef.Default.PrivateCacheDirectory(), "cache.sqlite.db");
});

// 同时注册一个 no-op 兜底，供「缓存彻底不可用」时（如外层 catch 到构造异常）切换。
// no-op 不是内存缓存（不占额外内存，Get 永远返回 null），区别于 §2 的"不内置内存降级"。
services.AddSingleton<NoOpDistributedCache>();
```

> no-op 实现放 Polymerium 侧（`src/Polymerium.Avalonia/Services/NoOpDistributedCache.cs`），不属于 TridentCore.Caching——它是 Polymerium 自己的策略选择。库不规定策略。

实际注册时的构造期 try-catch 由 Polymerium 自行决定是否需要（若希望构造失败时也能启动）。**本计划只交付库本身**，Polymerium 侧适配在库落地后单独处理。

---

## 6. 验收标准

| # | 场景 | 期望 |
|---|------|------|
| 1 | 无 db 文件首次启动 | 建库建表，读写正常 |
| 2 | 有健康 db 启动 | `Probe` 通过，直接复用，不重建 |
| 3 | db 损坏（乱写字节）| `Probe` 失败 → 删主 db（DELETE 模式无旁车）→ 重建 → 读写正常 |
| 4 | db 文件被独占锁，删不掉 | 库抛明确异常（`SqliteCacheInitializationException`）；不在库内吞掉 |
| 5 | 旧 NeoSmart `cache.sqlite.db` 残留 | `Probe` 不识别旧 schema（列名不同）→ 当作不兼容 → 删除重建。数据丢失可接受 |
| 6 | 单次 `Set/Get/Remove` 抛 `SqliteException` | 操作级 catch，返回 null/静默；后续操作不受影响 |
| 7 | 写冲突 `SQLITE_BUSY` | 按 `MaxRetryOnLock` 重试退避后成功 |
| 8 | 后台清理按 `CleanupInterval` 执行 | 过期条目被清理，统计条目数下降 |
| 9 | `IBulkableCache.GetMany` 100 键 | 单事务，往返 1 次 |
| 10 | `RepositoryAgent` 行为不变 | 缓存命中率与 NeoSmart 时期相当或更优 |

场景 4 是与 POLYMERIUM-22 行为的关键差异：**库抛异常，但消费方（Polymerium）catch 后用 no-op 替代 → 应用不崩**。场景 5 复现 POLYMERIUM-1C 的数据态，验证不再撞"table already exists"。

---

## 7. 风险与取舍

| 风险 | 取舍 |
|------|------|
| DELETE 模式写锁全库，高并发写下吞吐低于 WAL | 缓存场景读多写少，单线程 SQLite 吞吐远超需求；换来的"无常驻旁车文件"价值更高 |
| 库抛异常，消费方忘 catch = 崩溃 | 这是明确契约，文档与 XML 注释强调；Polymerium 注册层提供 no-op fallback 示范 |
| 不兼容旧库，用户升级时缓存全部失效 | 缓存数据非持久资产，重建无感；换取的"完美 schema"长期收益更高 |
| 库自身未来 schema 升级需要迁移逻辑 | `cache_meta.schema_version` 已预留；升级路径在库内部，对消费方透明 |
| 新增 SQLite 依赖到 Trident（此前无） | 与 Polymerium 已有依赖重叠（Microsoft.Data.Sqlite 同版本），不引入额外原生库 |

---

## 8. 不做的事（明确边界）

- **不兼容 NeoSmart 旧库 schema** —— 全新设计，不为旧格式留兼容代码
- **不用 WAL** —— DELETE 模式，从机制上消灭旁车文件残留类 bug
- **不在库内做内存降级** —— 失败抛异常，策略交给消费方
- **不做分布式** —— 单机持久化
- **不实现 `IDistributedCache` 以外的网络/序列化封装** —— 库只做 KV 字节缓存，序列化由消费方决定
- **不在此计划内交付 Polymerium 侧 no-op 适配** —— 库落地后单独处理

---

## 9. 触发实施的条件

满足任一可考虑排期：

- POLYMERIUM-1C / POLYMERIUM-22 在 Sentry 再次出现（单实例 + 现状仍不足）
- RepositoryAgent 需要 `GetMany`/`SetMany` 批量能力（多版本元数据拉取性能优化）
- 引入新的 `IDistributedCache` 消费方，需求面扩大值得投资

若都未发生，本计划可长期搁置——单实例闸门 + NeoSmart 现状已覆盖绝大多数真实场景。

实施时务必手动复现状景 4/5（POLYMERIUM-22/1C 的触发态），验证修复后据此关闭 Sentry issue（commit message 带 `Fixes POLYMERIUM-1C` / `Fixes POLYMERIUM-22`）。

---

## 附录：备选方案备案

本节记录主体设计在各关键决策点上**未采纳但可行**的备选方案，供未来实施时若主方案遇障可查。每个决策点标注「当前选」「备选」与「重启用条件」。

### A.1 Journal 模式

当前选：**DELETE**（§3.2）

| 备选 | 机制 | 重启用条件 |
|------|------|------------|
| **TRUNCATE** | rollback journal 事务后截断为 0 而非删除，省去文件反复创建/删除的 IO | 若压测发现 DELETE 模式下 journal 频繁创建删除带来可观 IO 开销 |
| **PERSIST** | journal 文件固定保留，内容覆写 | 一般不优于 DELETE，仅在与外部工具协调 journal 行为时考虑 |
| **WAL** | 读写并发，但需管理 `-wal`/`-shm` 常驻旁车 | **POLYMERIUM-1C 根因**。仅当未来出现高并发写需求、且能实现「连接关闭时 checkpoint 并清理旁车」的可靠生命周期时重评估 |
| **MEMORY** | journal 在内存，断电损坏 | 缓存场景仍需 crash safety（避免重启后又触发损坏重建循环），不适合 |
| **OFF** | 完全无 journal | 同上，否决 |

### A.2 SQLite 引擎依赖归属

当前选：**模块自带** `Microsoft.Data.Sqlite` + `SQLitePCLRaw.bundle_e_sqlite3`（§5.1）

| 备选 | 做法 | 重启用条件 |
|------|------|------------|
| **宿主提供** | 模块只引 `Microsoft.Data.Sqlite`，宿主负责 `SQLitePCLRaw` 初始化与原生库 | 若要让库更轻、避免与宿主原生库版本冲突；代价是库不能独立运行，文档需强约束宿主义务 |
| **复用 Polymerium 已引入的 SQLite** | — | **不可行**：SQLite 在 Polymerium 侧（NeoSmart/FreeSql 间接引入），Trident 内部拿不到；依赖方向反，否决 |
| **改用 FreeSql 抽象** | 不直接用 `Microsoft.Data.Sqlite`，走 FreeSql | FreeSql 是 ORM，不是 KV 缓存工具，且 TridentCore.Caching 不应耦合特定 ORM；否决 |

### A.3 库层失败策略

当前选：**构造失败抛异常**（§3.3，纯净契约）

| 备选 | 做法 | 重启用条件 |
|------|------|------------|
| **可配置 OnFailure = Throw / NoOp** | 选项让消费方选 | **用户已否决**（API 复杂）。若未来面向「不关心策略的大众用户」重新定位可重评估 |
| **内置 no-op 降级** | 库内部 catch 后返回空实现 | **用户已否决**（内存翻倍/语义混淆）。永不重启用 |
| **有限重试后抛** | `SafeDelete` 内部对 `IO` 异常重试 N 次再抛 | **已作为实现细节采纳**，不改变顶层「抛异常」契约 |
| **延迟初始化** | 构造不连接，首次操作才 Open | 与「启动期验证」诉求冲突（启动就想知道缓存可用与否）；但可作 A.4 消费侧策略 |

### A.4 Polymerium 侧适配策略（库落地后单独决策）

当前推荐：**no-op fallback**（§5.2）

| 备选 | 做法 | 重启用条件 |
|------|------|------------|
| **延迟初始化** | 缓存服务首次被调用时才构造，避开启动关键路径 | 若希望启动绝对不因缓存故障而受阻；代价是首次 HTTP 请求前才发现缓存不可用 |
| **重试启动** | 构造失败 → 延时后重试若干次 → 仍失败才换 no-op | 若观察到锁冲突多为短暂（杀软扫描几秒后释放）；代价是启动时间变得不确定 |
| **让它崩** | 不做适配，构造失败 = 启动失败，用户重启 | 体验最差，**与修复 POLYMERIUM-22 的初衷冲突**，否决 |

### A.5 功能增强（§4 之外的未选特性）

§4 已选：批量 / 统计 / 维护。以下为未来可加：

| 特性 | 内容 | 重启用条件 |
|------|------|------------|
| **LRU + 容量上限** | 当前仅 TTL 淘汰，无容量约束 | 若观察到 cache.db 无限膨胀（恶意/异常写入），需加容量上限 + 最近最少使用淘汰 |
| **事件钩子** | `OnHit` / `OnMiss` / `OnEvict` 事件 | 若调试或可观测需求增强（如逐键追踪缓存行为） |
| **IHealthCheck 实现** | 接入 `Microsoft.Extensions.Diagnostics.HealthChecks` | 若 Polymerium 引入健康检查仪表盘 |
| **多分区 namespace** | 同一 db 内多消费方分区（key 前缀隔离） | 若多个 `IDistributedCache` 消费方共用单 db 且需独立清理 |
| **序列化泛型封装** | `ICacheSerializer<T>` 之类 | **当前不做**：库只存 `byte[]`，序列化是消费方职责；未来可作为独立扩展层叠加，不进核心 |
| **压缩** | 大 value 写入前 gzip | 若缓存内容多为可压缩文本（JSON metadata）且体积成为 IO 瓶颈 |

### A.6 表结构变体

当前选：单表 `cache_entries` + `cache_meta`（§3.1）

| 备选 | 做法 | 重启用条件 |
|------|------|------------|
| **分桶表** | 按键哈希分到多张子表 | 缓存条目达百万级、单表成为瓶颈时；当前规模远未到此 |
| **带 TTL 物化视图** | 用物化视图加速过期扫描 | 部分索引（`WHERE expires_at IS NOT NULL`）已足够，无需 |
| **内容寻址（content-addressed）** | value 哈希入内容表，key 表只存指针 | 若大量 key 共享相同 value（去重收益）；RepositoryAgent 场景去重率低，不值得 |
