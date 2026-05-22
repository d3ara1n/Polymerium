# 实例快照功能

## 概述

快照用于在实例编辑过程中保存当前状态，包括配置（Rice）、文件引用（import/persist/live）。
用户可以在修改整合包中途拍摄快照，后续可回退到任意快照点。

## 存储布局

```
instances/<key>/
├── profile.json
├── import/
├── live/
├── persist/
├── build/
└── snapshots/
    ├── data.db                    # 实例专属 SQLite 数据库
    └── objects/                   # 内容寻址存储
        └── ab/
            └── abcdef...          # SHA1 hash，前 2 字符作子目录
```

快照范围内需要纳入的目录/文件：

| 路径 | 纳入 | 说明 |
|---|---|---|
| `profile.json` 中的 `Rice` | ✅ | 序列化为 Metadata（不含 Name、Overrides） |
| `import/` | ✅ | 整合包编辑输入源 |
| `live/` | ✅ | 运行时活跃文件 |
| `persist/` | ✅ | 持久化文件 |
| `build/` | ❌ | 可由部署重建 |
| `snapshots/` | ❌ | 快照系统自身 |
| `icon.*` | ❌ | 实例图标 |

## 分层架构

```
TridentCore.Abstractions
├── ISnapshotStore                # 存储接口（应用层实现）
├── SnapshotRecord                # 快照数据库行模型
├── ReferenceRecord               # 引用数据库行模型
├── SnapshotInfo                  # 列表展示用只读模型
├── SnapshotDetail                # 详情预览用只读模型
├── SnapshotFileEntry             # 单个文件引用信息
├── GcResult                      # GC 结果
└── GcStatistics                  # GC 统计预览

TridentCore.Core
├── SnapshotManager               # 单例，工厂方法 Open(key)
└── InstanceSnapshots             # scoped handle，IDisposable

Polymerium.App
└── FreeSqlSnapshotStore          # ISnapshotStore 的 FreeSql 实现
```

应用层可自由选择存储实现：Polymerium 用 FreeSql + SQLite，Trident.Cli 可以用 JSON 文件。

## 数据库表结构

### Snapshot 表

| 字段 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT (PK) | Guid |
| `Label` | TEXT | 用户自定义名称，可 NULL |
| `Remark` | TEXT | 用户备注，可 NULL |
| `Metadata` | TEXT NOT NULL | JSON：序列化的 `Rice` 对象 |
| `PackageCount` | INTEGER | 包数量（预览用） |
| `FileCount` | INTEGER | 文件数量（预览用） |
| `TotalSize` | INTEGER | 总大小 bytes（预览用） |
| `CreatedAt` | TEXT NOT NULL | ISO 8601 |

### Reference 表

| 字段 | 类型 | 说明 |
|---|---|---|
| `Id` | TEXT (PK) | Guid |
| `SnapshotId` | TEXT NOT NULL | FK → Snapshot.Id |
| `Hash` | TEXT NOT NULL | SHA1 hex |
| `Path` | TEXT NOT NULL | 相对于实例根目录的路径（`/` 分隔） |
| `Size` | INTEGER | 文件大小 bytes |
| `ModifiedAt` | TEXT NOT NULL | 原始文件 mtime（ISO 8601） |

索引：
- `idx_reference_snapshot` ON `Reference(SnapshotId)`
- `idx_reference_hash` ON `Reference(Hash)`

### 引用去重语义

Reference 行是 **per-snapshot** 的，每个快照有自己完整的引用列表。
两个不同快照可能包含 Hash 相同的 Reference 行，它们在物理上指向同一个 object 文件。
去重发生在 object 文件层面（内容寻址），而非 Reference 行层面。

## Metadata JSON

Metadata 存 `Profile.Rice`（不含 Name 和 Overrides），序列化后形如：

```json
{
  "Source": "curseforge:12345",
  "Version": "1.20.1",
  "Loader": "fabric:0.15.11",
  "Packages": [
    { "Purl": "curseforge:12345:67890", "Enabled": true, "Source": null, "Tags": ["client"] }
  ],
  "Rules": [
    { "Selector": { "Type": "Purl", "Purl": "curseforge:*:*" }, "Enabled": true, "Destination": null, "Skipping": false, "Normalizing": false }
  ]
}
```

## ISnapshotStore 接口

```csharp
public interface ISnapshotStore : IDisposable
{
    /// <summary> 原子写入快照及其全部引用记录 </summary>
    void Save(SnapshotRecord snapshot, IEnumerable<ReferenceRecord> references);

    /// <summary> 按创建时间倒序返回全部快照（不加载引用） </summary>
    IReadOnlyList<SnapshotRecord> LoadSnapshots();

    /// <summary> 加载单个快照 </summary>
    SnapshotRecord? LoadSnapshot(Guid id);

    /// <summary> 加载一个快照的全部引用 </summary>
    IReadOnlyList<ReferenceRecord> LoadReferences(Guid snapshotId);

    /// <summary> 原子删除快照及其全部引用 </summary>
    void DeleteSnapshot(Guid id);

    /// <summary> 返回所有仍被引用的 hash 集合（GC 用） </summary>
    ISet<string> GetAllReferencedHashes();
}
```

## SnapshotManager

```csharp
public class SnapshotManager
{
    private readonly Func<string, ISnapshotStore> _storeFactory;

    public SnapshotManager(Func<string, ISnapshotStore> storeFactory)
    {
        _storeFactory = storeFactory;
    }

    /// <summary>
    /// 打开一个实例的快照会话，返回 scoped handle。
    /// 调用方负责 Dispose（Modal 关闭时）。
    /// </summary>
    public InstanceSnapshots Open(string instanceKey)
    {
        var store = _storeFactory(instanceKey);
        return new InstanceSnapshots(instanceKey, store);
    }
}
```

DI 注册方式（Polymerium）：

```csharp
services.AddSingleton<SnapshotManager>(_ =>
    new SnapshotManager(key => new FreeSqlSnapshotStore(key))
);
```

## InstanceSnapshots (scoped handle)

```csharp
public sealed class InstanceSnapshots : IDisposable
{
    private readonly string _key;
    private readonly ISnapshotStore _store;

    internal InstanceSnapshots(string key, ISnapshotStore store)
    {
        _key = key;
        _store = store;
    }

    // ===== 快照创建 =====

    /// <summary>
    /// 拍摄快照。扫描 import/persist/live，写入 objects，原子提交 DB。
    /// </summary>
    Task<SnapshotInfo> CreateAsync(
        string? label,
        string? remark,
        CancellationToken token);

    // ===== 快照列表 =====

    /// <summary> 获取快照列表（轻量，按创建时间倒序）。 </summary>
    IReadOnlyList<SnapshotInfo> List();

    // ===== 快照详情 =====

    /// <summary> 获取完整详情（含 Rice + 文件引用列表）。 </summary>
    SnapshotDetail GetDetail(Guid snapshotId);

    // ===== 快照删除 =====

    /// <summary> 删除单个快照（引用行随之删除，objects 不立即清理）。 </summary>
    void Delete(Guid snapshotId);

    // ===== 垃圾回收 =====

    /// <summary> 删除不被任何 Reference 引用的孤立 object 文件。 </summary>
    GcResult CollectGarbage();

    /// <summary> 获取孤立 object 统计（不实际删除）。 </summary>
    GcStatistics GetGarbageStatistics();

    // ===== 生命周期 =====

    public void Dispose() => _store.Dispose();
}
```

## FreeSqlSnapshotStore (Polymerium 实现)

```csharp
public class FreeSqlSnapshotStore : ISnapshotStore
{
    private readonly IFreeSql _freeSql;

    public FreeSqlSnapshotStore(string instanceKey)
    {
        var path = PathDef.Default.FileOfSnapshotDatabase(instanceKey);
        var dir = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);

        _freeSql = new FreeSqlBuilder()
            .UseConnectionString(DataType.Sqlite, $"Data Source=\"{path}\";Cache=Private")
            .UseAutoSyncStructure(true)
            .Build();
    }

    public void Save(SnapshotRecord snapshot, IEnumerable<ReferenceRecord> references)
    {
        _freeSql.Transaction(() =>
        {
            _freeSql.Insert(snapshot).ExecuteAffrows();
            _freeSql.Insert(references.ToArray()).ExecuteAffrows();
        });
    }

    public IReadOnlyList<SnapshotRecord> LoadSnapshots() =>
        _freeSql.Select<SnapshotRecord>()
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

    public SnapshotRecord? LoadSnapshot(Guid id) =>
        _freeSql.Select<SnapshotRecord>()
            .Where(x => x.Id == id.ToString("D"))
            .First();

    public IReadOnlyList<ReferenceRecord> LoadReferences(Guid snapshotId) =>
        _freeSql.Select<ReferenceRecord>()
            .Where(x => x.SnapshotId == snapshotId.ToString("D"))
            .ToList();

    public void DeleteSnapshot(Guid id) =>
        _freeSql.Transaction(() =>
        {
            var idStr = id.ToString("D");
            _freeSql.Delete<ReferenceRecord>()
                .Where(x => x.SnapshotId == idStr)
                .ExecuteAffrows();
            _freeSql.Delete<SnapshotRecord>()
                .Where(x => x.Id == idStr)
                .ExecuteAffrows();
        });

    public ISet<string> GetAllReferencedHashes() =>
        new HashSet<string>(
            _freeSql.Select<ReferenceRecord>()
                .ToList()
                .Select(x => x.Hash),
            StringComparer.OrdinalIgnoreCase
        );

    public void Dispose() => _freeSql?.Dispose();
}
```

## 快照创建工作流

```
InstanceSnapshots.CreateAsync(label, remark, token)
│
├── 阶段 1: 扫描 + 写入 objects（幂等文件 I/O，可中断无副作用）
│   ├── 读取 profile 获取 Rice → 序列化为 Metadata JSON
│   ├── 扫描 import/、persist/、live/ 目录
│   ├── 对每个文件：
│   │   ├── 计算 SHA1 hash
│   │   ├── 目标路径 = objects/{hash[..2]}/{hash}
│   │   ├── 若文件已存在且大小一致 → 跳过（幂等）
│   │   └── 否则 → 写入 .tmp → rename 到目标路径（原子）
│   └── 收集 (relativePath, hash, size, modifiedAt) 列表
│
├── 构建 SnapshotRecord（含 PackageCount, FileCount, TotalSize）
│
└── 阶段 2: _store.Save(snapshot, references)
    └── FreeSql.Transaction 包裹
        ├── 成功 → 快照创建完成，返回 SnapshotInfo
        └── 失败 → 自动 Rollback，等于快照未创建
            └── 阶段 1 残留的孤立 objects 由 GC 清理
```

### 回滚保证

无需额外回滚机制，由两阶段设计天然保证：

| 失败点 | 后果 | 处理 |
|---|---|---|
| 阶段 1 中途崩溃 | objects/ 中有若干已写入文件，但 DB 无记录 | GC 时清理孤立文件 |
| 阶段 1 写入单个 object 失败 | 先写 `.tmp` 后 rename，失败时 `.tmp` 残留或文件不存在 | 无影响，可重试 |
| 阶段 2 事务失败 | Snapshot 和 Reference 行全部回滚 | 等于快照未创建 |
| 阶段 2 提交后崩溃 | 快照已完整创建 | 正常完成 |

## 垃圾回收 (GC)

手动触发（UI 按钮），不自动执行。

```
InstanceSnapshots.CollectGarbage()
│
├── referenced = _store.GetAllReferencedHashes()
│   └── SELECT DISTINCT Hash FROM Reference
│
├── 扫描 objects/ 目录下所有文件
│
└── 对每个文件：hash 不在 referenced 集合中 → 删除
    └── 清理空的子目录
```

`GetGarbageStatistics()` 逻辑相同但不删除，仅返回统计信息供 UI 展示。

## PathDef 扩展

在 `TridentCore.Abstractions/PathDef.cs` 的 `#region Instance Folder` 中新增：

```csharp
public string DirectoryOfSnapshots(string key) =>
    Path.Combine(InstanceDirectory, key, "snapshots");

public string FileOfSnapshotDatabase(string key) =>
    Path.Combine(DirectoryOfSnapshots(key), "data.db");

public string DirectoryOfSnapshotObjects(string key) =>
    Path.Combine(DirectoryOfSnapshots(key), "objects");
```

## 哈希算法

使用 **SHA1**。与项目现有 `FileHelper.VerifyModified(path, null, sha1)` 使用的算法一致。

## UI 入口（后续实现）

- 入口：实例页面左侧快捷按钮组 → 打开 `InstanceSnapshotModal`
- Modal 使用 `ModalModel` 模式（`InstanceSnapshotModalModel`）
- Modal 生命周期内通过 `using var snapshots = snapshotManager.Open(key)` 持有 handle
- Modal 关闭时自动 Dispose 释放 DB 连接

## 快照恢复（后续实现）

`InstanceSnapshots.RestoreAsync(snapshotId, token)` 在后续迭代中设计。
需要考虑：全量替换 vs 增量合并、恢复前自动创建当前状态快照、实例状态检查（禁止在运行中恢复）等。
