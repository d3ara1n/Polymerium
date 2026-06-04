# Polymerium 快照 Store 设计

## 状态

Implementation Ready Draft。

本文档描述 Polymerium 的实例级快照 Store。它不描述旧的 TRIDENT.CLI 快照 Store，也不以 `docs/TRIDENT_V2.md` 中已经过时的快照说明作为设计依据。

## 范围

Polymerium 在 `TridentCore.Abstractions.Snapshots` 的业务抽象之上实现自己的快照 Store。

`TridentCore.Abstractions.Snapshots` 定义业务数据模型。Polymerium Store 自己定义终端储存模型、持久化布局、数据库结构，以及数据库记录和业务模型之间的转换。

Store 以单个实例为作用域。`SnapshotsModal` 打开当前实例的 Store，并在 Modal 生命周期内维护和释放它。

## 非目标

- 不实现或描述 TRIDENT.CLI 的快照 Store。
- 不把快照作为整合包导出格式。
- 整合包导出不包含快照。
- 不支持手动复制、传播或迁移快照目录。
- 不捕获 `build`、缓存、assets、libraries、runtimes、package objects 或其他生成 / 全局文件。
- 不保留空目录。
- 不支持捕获目录中的 symlink。
- 不在快照 Modal 中提供快照 GC 入口。

## 捕获范围

只捕获实例下的以下目录：

- `live/`
- `import/`
- `persist/`

快照引用中的路径相对于实例根目录，并且直接包含 layer 名称，例如：

```text
import/config/options.txt
live/options.txt
persist/saves/world/level.dat
```

因此不需要单独的 source layer 字段。如果未来增加更多捕获根目录，也可以通过在相对路径中包含根目录名的方式保持兼容。

捕获时忽略空目录。还原后，`live`、`import` 和 `persist` 中遗留的空目录会被删除。

## Symlink 策略

`live`、`import` 和 `persist` 是托管内容根目录，内部不允许存在 symlink。

捕获行为：

- 扫描捕获目录时如果发现 symlink，捕获失败。
- 不检查 symlink target。
- 不把 symlink 记录为 reference。

还原行为：

- 如果还原目标或还原所需的父级路径是 symlink，还原失败。
- 还原过程永远不跟随 symlink。

由于空目录会被忽略，symlink 又是非法状态，MVP 阶段的 reference 模型只需要表达普通文件。当前设计不需要 `ReferenceKind`。

## 业务模型

`TridentCore.Abstractions.Snapshots` 是快照业务模型的来源。

`SnapshotInfo` 仍然是快照级模型：

```csharp
public record SnapshotInfo(
    object Id,
    string Label,
    string Remark,
    Profile.Rice Metadata,
    int PackageCount,
    int FileCount,
    long TotalSize,
    DateTime CreatedAt);
```

`ReferenceInfo` 应在 Trident.Net 中扩展 `FileAttributes`，因为文件属性会影响还原语义，不只是 Polymerium Store 的实现细节：

```csharp
public record ReferenceInfo(
    object Id,
    string Hash,
    string RelativePath,
    long Size,
    DateTime LastModified,
    FileAttributes Attributes);
```

Polymerium 使用 `Guid` 作为 snapshot 和 reference 的具体 `Id` 类型。

快照 metadata 只保存 `Profile.Rice`。当前设计不需要 schema version 字段。

快照时间统一使用本地 `DateTime`。快照操作不涉及联网同步，因此不需要 `DateTimeOffset`。

## Hash

快照对象使用 SHA1 作为固定内容寻址算法。

算法是 Store 标准的一部分，不在 object 或 reference 中额外记录。

如果 object store 中已存在相同 SHA1 的对象，但其 size 与当前提交的文件不一致，Store 视为快照存储损坏并中止操作。

Hash 策略：

- 小文件可以在内存中 hash。
- 大文件应使用流式 IO hash。
- Commit 阶段应根据文件大小选择合适的流式 hash-and-copy 路径。

## Store 布局

每个实例在自己的实例目录下拥有独立的快照 Store：

```text
snapshots/
  data.shot.db
  objects/
    ab/
      abcdef...
```

`data.shot.db` 是通过 FreeSql 访问的 SQLite 数据库。

对象文件不存入数据库，而是作为内容寻址文件保存：

```text
snapshots/objects/{sha1[0..2]}/{sha1}
```

数据库是 snapshot 和 reference 的权威索引。object 文件可能因为 commit 失败或删除快照而成为 orphan。orphan object 由手动 GC 回收。

## 终端储存模型

Polymerium 的数据库结构是 Store 实现细节。它负责和 Trident 业务模型互相转换。

建议记录结构：

```text
SnapshotRecord
- Id: Guid
- Label: string
- Remark: string
- MetadataJson: string/blob
- PackageCount: int
- FileCount: int
- TotalSize: long
- CreatedAt: DateTime

SnapshotReferenceRecord
- Id: Guid
- SnapshotId: Guid
- Hash: string
- RelativePath: string
- Size: long
- LastModified: DateTime
- Attributes: int

SnapshotObjectRecord
- Hash: string
- Size: long
- CreatedAt: DateTime
```

`SnapshotObjectRecord` 是可选但推荐的表。它能让 object size 校验、GC 预览和损坏检测比只扫描文件系统更便宜。

推荐索引：

```text
SnapshotRecord.Id primary key
SnapshotReferenceRecord.Id primary key
SnapshotReferenceRecord.SnapshotId index
SnapshotReferenceRecord.Hash index
SnapshotReferenceRecord.SnapshotId + RelativePath unique index
SnapshotObjectRecord.Hash primary key
```

快照列表 UI 只加载 `SnapshotRecord`。reference 明细只在显示预览 / 详情或执行还原时懒加载。

## 拍摄预览

拍摄预览不修改文件系统，也不修改数据库。

用户在创建页点击“拍摄”时，Store 扫描 `live`、`import` 和 `persist`，校验路径、拒绝 symlink，并构建内存中的预览模型。这个步骤只是统计和检查，不写 object、不插入数据库记录，也不实际物化快照数据。

预览模型包含足够用户检查未来快照的信息，避免 UI 即时重复扫描：

- 拟生成的快照级展示信息。
- 拟生成的文件条目。
- 文件数量。
- 包数量。
- 总大小。
- 相对路径。
- 文件大小。
- LastModified。
- FileAttributes。

预览保存在页面模型中。只有用户明确提交快照时才会持久化。由于预览阶段不计算或存储 object hash，它不是最终的 `ReferenceInfo` 列表。最终的 `SnapshotInfo` 和 `ReferenceInfo` 在 commit 阶段根据实际入库的文件生成。

如果文件在预览后、commit 前发生变化，视为用户责任。快照操作只允许在实例 Idle 时执行，Polymerium 不尝试防御 Modal 会话期间的外部文件修改。

## Commit 流程

Commit 是写入 object 文件和数据库记录的阶段。

流程：

```text
1. 用户在已有拍摄预览后点击提交 / 保存。
2. Store 确认实例仍允许执行快照操作。
3. Store 开启 SQLite transaction。
4. 对每个预览文件条目，Store 读取当前源文件。
5. Store 对文件内容 hash，并复制到 object store。
6. object 写入使用临时文件加原子 rename。
7. 如果 object 已存在且 hash 和 size 匹配，复用已有 object。
8. 如果 object 已存在、hash 相同但 size 不同，视为损坏并中止 commit。
9. Store 根据实际提交的文件生成最终 SnapshotInfo 和 ReferenceInfo。
10. Store 写入 SnapshotRecord。
11. Store 批量插入 SnapshotReferenceRecord。
12. 如果存在 SnapshotObjectRecord 表，Store 写入或更新对象记录。
13. Store 提交 transaction。
```

数据库写入由 SQLite transaction 保证一致性。object 写入通过临时文件加 rename 避免半文件。如果 object 文件已经写入但数据库事务失败，这些 object 会成为 orphan，并在后续 GC 中清理。

## 还原流程

还原是 best-effort 且可重试的操作。不尝试做完整回滚。

流程：

```text
1. 只有 Idle 实例允许还原。
2. 用户选择快照。
3. Store 读取 SnapshotRecord 和 SnapshotReferenceRecord。
4. Store 校验被引用的 object 存在。
5. Store 校验 object size 与 reference size 匹配。
6. Store 使用快照 metadata 替换当前 Profile.Rice。
7. Store 将每个 object 复制到实例根目录下对应的 RelativePath。
8. Store 恢复 LastModified。
9. Store 恢复 FileAttributes。
10. Store 删除 live/import/persist 中不属于该快照 references 的普通文件。
11. Store 从深到浅删除 live/import/persist 中的空目录。
12. 如果失败，用户可以重试还原。
```

还原前是否拍摄一个新快照由用户决定。Store 不自动创建还原前快照。

## 删除与 GC

删除快照只删除数据库记录：

- 删除该快照对应的 `SnapshotReferenceRecord`。
- 删除 `SnapshotRecord`。
- 不立即删除 object 文件。
- 不从快照 Modal 中运行 GC。

GC 是手动维护操作，入口在其他地方，例如设置 / 存储管理区域。快照 Store 只暴露所需查询能力，例如所有已引用 hash；快照 Modal 不提供 GC 入口。

GC 行为：

```text
1. 从数据库枚举所有被引用的 hash。
2. 枚举 snapshots/objects 下的 object 文件。
3. 删除没有被任何快照引用的 object 文件。
4. 可选地删除空的 object prefix 目录。
```

## 生命周期与并发

只有 `Idle` 实例允许创建、提交、还原或删除快照。

UI 应在非 Idle 状态锁定快照入口。Store / service 层仍应做状态校验，避免未来其他调用点破坏约束。

`SnapshotsModal` 是 Store 的生命周期边界：

- 打开 Modal：打开该实例的 Store。
- 关闭 Modal：释放 Store。

实例部署、更新、安装或游戏运行时，不允许执行快照操作。

## 性能说明

SQLite 可以承受预期的 reference 数量。

预期量级：

```text
1 个快照 x 5,000 文件 = 5,000 reference rows
20 个快照 x 5,000 文件 = 100,000 reference rows
100 个快照 x 10,000 文件 = 1,000,000 reference rows
```

只要批量插入使用 transaction，并且索引合理，这属于 SQLite 的正常使用范围。

规则：

- 不要每条 reference 单独提交事务。
- 不要把文件内容存进 SQLite。
- 快照列表 UI 不要急切加载所有 snapshot 的 references。
- 使用 transaction 批量插入。
- 对 `SnapshotId` 和 `Hash` 建索引。
- 文件内容保存在 object store 中。

## 安全要求

设计要求实现安全围栏和跨平台路径处理，但本文档不规定每个实现细节。

要求：

- 规范化所有 relative path。
- 拒绝 path traversal。
- 确保还原目标位于 `live`、`import` 或 `persist` 内。
- 捕获和还原时拒绝 symlink。
- 还原时永远不跟随 symlink。
- 删除操作只允许发生在捕获根目录内。
- 处理 Windows 路径大小写不敏感行为。
- 一致处理非法路径名和保留名称。

## UI 契约

快照创建保持两阶段工作流：

```text
拍摄预览 -> 提交快照
```

创建页状态：

```text
未拍摄
- 用户可以编辑 label 和 remark。
- 用户可以点击拍摄。
- 提交按钮禁用。

已拍摄
- 显示预览数据。
- 用户可以重新拍摄。
- 用户可以提交。

提交中
- 禁用输入。
- 显示进度或 busy 状态。

已提交
- 快照出现在快照列表中。
```

管理页行为：

- 列表只显示 snapshot records。
- 选中快照后懒加载 references 作为详情。
- 搜索可以按 label、remark、created time、game version 和 loader 过滤，前提是这些信息由 metadata 暴露。
- 还原和删除需要确认。

## 后续工作

以下功能有价值，但不是初始 Polymerium 快照 Store 实现的必要部分：

- 快照差异视图。
- 自动快照策略。
- 保留策略。
- 快照导入 / 导出。
- 压缩。
- 敏感文件排除。
- 云同步。
- 文件树预览。
- 从快照生成整合包 overrides。
