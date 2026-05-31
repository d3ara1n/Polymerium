# GC：孤儿数据调查报告

## 背景

项目运行时在多个位置产生孤儿数据：数据库行和磁盘文件会在父实体（实例、账号、快照等）被删除后残留。需要一个垃圾收集（GC）功能来清理这些数据，入口将设在设置页面的维护板块。

---

## 1. Persistence 数据库 — 实例 Key 关联的孤儿行

**数据库位置**: `~/.trident/.polymerium/persistence.sqlite.db`  
**注册位置**: `src/Polymerium.App/ServiceCollectionExtensions.cs`

实例删除时只清理磁盘目录（通过 `_bomb_has_been_planted_` 标记在下次启动时删除），但以下 5 张表中以实例 Key 为索引的行不会被清理：

| 表 | Key 列 | 孤儿内容 | 定义位置 |
|---|--------|---------|---------|
| `AccountSelector` | `Key` | 实例→账号 UUID 映射 | `PersistenceService.cs` |
| `Action` | `Key` | 编辑历史记录 | `PersistenceService.cs` |
| `Activity` | `Key` | 游玩时长、启动次数、崩溃次数 | `PersistenceService.cs` |
| `ViewState` | `Key` | 实例设置页状态 | `PersistenceService.cs` |
| `WidgetLocalSection` | `Key` | 小部件数据（备注、网络检测、开发工具箱等） | `PersistenceService.cs` |

**风险**: 如果用户删除名为 "MyModpack" 的实例（key=`mymodpack`），之后又创建同名新实例，新实例会继承所有残留的旧数据。

**现有清理方法**: `PersistenceService` 已有 `ClearActions(key)` 和 `ClearActivities(key)` 方法，但在实例删除流程中未被调用。

---

## 2. Persistence 数据库 — Account 删除后的孤儿

**删除入口**: `src/Polymerium.App/PageModels/AccountsPageModel.cs` (line 133)

当 `RemoveAccount(uuid)` 被调用时，只删除 `Account` 表中的记录。`AccountSelector` 表中引用该账号 UUID 的行成为悬空引用，且无对应实例来清理它们。

---

## 3. 快照数据库 + 快照文件系统

**数据库位置**: `~/.trident/instances/{key}/snapshots/snapshots.sqlite.db`（每个实例一个）  
**定义位置**: `src/Polymerium.App/Snapshots/SnapshotStore.cs`

### 3a. ReferenceRecord 孤儿

当 `SnapshotRecord` 被删除后，其关联的 `ReferenceRecord` 行（通过 `SnapshotId` 外键关联）变成无主记录。

### 3b. 快照物理文件孤儿

文件存储路径: `~/.trident/instances/{key}/snapshots/objects/{hash_prefix}/{hash}`

快照采用 content-addressable 存储模式。当快照被删除时：
- `ReferenceRecord` 行被从数据库移除
- 但被引用的物理文件留在磁盘上，**不会被回收**

`SnapshotStore` 已有 `GetAllReferencedHashes()` 方法返回所有仍被引用的哈希值，可以用它与磁盘文件做差集来识别孤儿文件，但**目前没有代码执行这个清理**。

> 注：对于已删除的实例，整个实例目录会在下次启动时被删除（包括快照），所以此问题仅影响长期存在且频繁创建/删除快照的实例。

---

## 4. 缓存目录

**根路径**: `~/.trident/cache/`  
**路径定义**: `submodules/Trident.Net/src/TridentCore.Abstractions/PathDef.cs`

| 路径 | 内容 | 孤儿场景 |
|------|------|---------|
| `cache/icons/{prefix}/{hash}` | 图标位图缓存 | 实例删除后图标残留，仅靠 30 天 TTL 惰性过期，来源: `DataService.cs` |
| `cache/packages/{label}/{ns?}/{pid}/{vid}.{ext}` | Mod/Modpack 下载文件 | 实例删除或版本更新后不再需要 |
| `cache/assets/indexes/{index}.json` + `objects/{prefix}/{hash}` | Minecraft 资源 | 版本切换后旧版本资源可能残留 |
| `cache/libraries/{ns}/{name}/{ver}/{name}-{ver}-{platform}.{ext}` | Java 库 | 版本/实例删除后可能不再被任何实例引用 |
| `cache/runtimes/{major}/` | Java 运行时 | 无引用追踪，实例删除后可能不再需要 |

---

## 5. HTTP 缓存数据库

**数据库位置**: `~/.trident/.polymerium/cache.sqlite.db`  
**注册位置**: `src/Polymerium.App/Startup.cs` (line 129-140)

NeoSmart 的 HTTP 响应缓存。实例删除后其中与该实例包数据相关的缓存条目变成无用数据。由缓存策略控制淘汰，但不会主动清理。

---

## 实例删除流程（参考）

**触发**: `InstancePropertiesPageModel.DeleteInstance()` (`src/Polymerium.App/PageModels/InstancePropertiesPageModel.cs:264`)

```
Step 1 (当前会话):
  创建 bomb 文件: instances/{key}/_bomb_has_been_planted_
  调用 ProfileManager.Remove(key) → 从内存中移除 ProfileHandle
  调用 ProfileManager.RequestKey(key) → 防止本次会话复用该 key

Step 2 (下次启动):
  ProfileManager 构造函数扫描 instances/ 目录
  对每个子目录:
    if bomb 文件存在 → Directory.Delete(true) 删除整个实例目录
    else → 正常加载 profile
```

**清理了**: 磁盘上的实例目录（profile、icon、build、live、snapshots 等全部）  
**未清理**: persistence.sqlite.db 中的 5 张表、缓存目录中的关联文件、HTTP 缓存

---

## 清理策略建议

### 安全清理（无副作用）

- [ ] persistence.sqlite.db 中 Key 不匹配任何现有实例的行（AccountSelector、Action、Activity、ViewState、WidgetLocalSection）
- [ ] persistence.sqlite.db 中 UUID 不匹配任何现有 Account 的 AccountSelector 行
- [ ] snapshots.sqlite.db 中 SnapshotId 不匹配任何 SnapshotRecord 的 ReferenceRecord 行
- [ ] snapshots/objects/ 中不被任何 ReferenceRecord 引用的物理文件
- [ ] cache/icons/ 中超过 TTL 且不被任何实例引用的图标文件

### 需谨慎的清理（需用户确认）

- [ ] cache/packages/ 中不被任何实例 lock 文件引用的包文件
- [ ] cache/assets/ 中不被任何实例引用的资源版本
- [ ] cache/libraries/ 中不被任何实例引用的库文件
- [ ] cache/runtimes/ 中不被任何实例引用的运行时
- [ ] cache.sqlite.db 中的过期/无用 HTTP 缓存条目

---

## 关键文件索引

| 文件 | 角色 |
|------|------|
| `src/Polymerium.App/Services/PersistenceService.cs` | 所有 FreeSql 实体定义 + CRUD |
| `src/Polymerium.App/Snapshots/SnapshotStore.cs` | SnapshotRecord + ReferenceRecord 实体 |
| `src/Polymerium.App/Snapshots/SnapshotStoreFactory.cs` | 创建每个实例的快照 SQLite DB |
| `src/Polymerium.App/ServiceCollectionExtensions.cs` | FreeSql + persistence.sqlite.db 注册 |
| `src/Polymerium.App/Startup.cs` | NeoSmart cache.sqlite.db 注册 |
| `src/Polymerium.App/Services/DataService.cs` | 图标文件缓存、内存缓存 |
| `src/Polymerium.App/Services/WidgetHostService.cs` | 按 instance key 存储的小部件数据 |
| `src/Polymerium.App/PageModels/InstancePropertiesPageModel.cs` | 实例删除/重置触发点 |
| `submodules/Trident.Net/.../PathDef.cs` | 所有文件系统路径定义 |
| `submodules/Trident.Net/.../ProfileManager.cs` | Profile 生命周期、bomb 式删除 |
| `submodules/Trident.Net/.../InstanceManager.cs` | 实例部署/启动/更新/安装 |
