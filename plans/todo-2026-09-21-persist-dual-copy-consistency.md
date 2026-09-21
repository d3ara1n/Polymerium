# Persist 单文件双副本已知限制

立案日期：2026-09-21

## 状态

这是 Polymerium 当前模型的已知限制，不计划在部署或快照机制中修复。本文件用于维护限制说明，并在文档站具备统一的已知问题页面后同步到中英文网站文档。

## 限制

单文件 Local Data 通过符号链接投影到 Run Directory。游戏或其他程序可能先通过链接写入 Local Data，再删除链接并在同一路径创建普通文件，使 Run Directory 与 Local Data 暂时同时存在内容不同的两个副本。

投影清单只记录上次部署时 Local Data 的修改时间，无法证明两个副本的实际写入先后。因此当前行为存在以下边界：

- Local Data 的修改时间发生变化时，部署让 Local Data 胜出并删除 Run Directory 普通副本，即使后者可能更晚写入。
- Local Data 的修改时间未变化时，部署把 Run Directory 普通副本回迁到 Local Data。
- 快照保存 Local Data 和投影清单，但不保存尚未回迁的 Run Directory 普通副本；恢复后的下一次部署可能回迁恢复时仍留在 Run Directory 的内容。

这些行为无法仅凭现有文件和修改时间可靠消除。Run Directory 不是完整备份，用户应在创建快照前完成一次成功部署，并避免在 Local Data 与 Run Directory 同时保留需要独立保存的同路径内容。

## 文档同步

后续在 `website/content/docs/` 建立或整理已知问题说明时，将本限制同步到对应英文和中文页面，保持普通部署、Local Data 与快照边界的描述一致。
