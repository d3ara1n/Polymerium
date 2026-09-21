# 部署投影 v2 实现审查

立案日期：2026-09-21

## 审查状态

审查完成，findings 尚未处理。本文仅记录本轮实现的取证结果，不代表部署投影 v2 已完成验收。

审查范围包括父仓库与 `submodules/Trident.Net` 的未提交差异，并以 `todo-2026-09-21-import-projection-manifest-v2.md` 中确定的安全边界、失败重试、生命周期适配和验收要求为依据。

`dotnet build "Polymerium.slnx"` 在审查时成功，结果为 0 个警告、0 个错误；构建成功不替代真实实例验收。

## Findings

### High — 快照还原在内容恢复成功前丢弃当前归属状态

`SnapshotManager.RestoreAsync` 在移除链接和恢复文件之前调用 `ProjectionManifestHelper.Delete`，提前删除当前 import 与 persist 投影清单。此后若因取消、快照对象缺失或其他 I/O 错误中断，实例会同时失去原清单和快照清单。

后续部署会把源已删除但原本受管的 import 工作副本当作未知遗留文件；persist 单文件投影被普通文件替换时也会丢失用于 Delete-Create 判定的历史 mtime，转而触发未知双副本冲突。

闭合要求：快照内容未完整恢复前保留当前正式清单；预先验证快照携带的清单，并将正式清单的替换或删除作为还原的最后提交步骤。处理方式仍须遵守本任务“不引入完整文件系统事务”的边界。

### High — 整合包更新成功后错误清除 persist 仲裁历史

`InstanceManager.UpdateAsync` 成功路径调用 `ProjectionManifestHelper.Delete`，当前该方法同时删除 import 与 persist 两份清单。

整合包更新会替换 Pack Source，但不会使 persist 单文件投影及其 mtime 基线失效。若游戏此前以 Delete-Create 将 persist 链接替换为 build 普通文件，更新后删除 persist 清单会让下一次部署无法证明该普通文件的来源，原本应回迁的内容会变成冲突。

闭合要求：成功更新只清除因 Pack Source 更换而失效的 import 归属记录，保留仍描述当前 persist 投影历史的状态；失败回滚继续保持两份原记录不变。

### High — import 目录到文件转换在中断后不能重新规划收敛

`SolidifyManifestStage.RemoveImportFile` 删除受管文件后不清理已空的受管父目录；`DeploymentPlanner.DirectoryContainsOnlyTrackedImports` 又把已经变空的目录判断为无法证明归属。

例如旧清单记录 `foo/bar`，新 import 改为文件 `foo`：执行在删除 `foo/bar` 后中断会留下空的 `build/foo`。重试仍读取旧清单，但规划器拒绝这个空目录，无法继续创建新文件。整合包更新备份旧工作副本时也会留下空目录壳，成功更新并清除 import 清单后，下一次部署同样可能在文件／目录转换处失败。

闭合要求：删除或备份已确认的 import 工作副本后，只在 build 边界内清理可证明为空的父目录；重新规划时应能依据旧清单中的后代归属识别中断留下的空目录，同时不得扩大到未知目录或未知内容。

### Medium — 仅含符号链接的目录被误判为真实对象冲突

`DeploymentPlanner.DirectoryContainsOnlyTrackedImports` 跳过符号链接，但没有把它们计为可丢弃的投影实体。因此，一个只包含旧 package 或 persist 链接的普通目录会返回不可替换，阻止最终文件投影接管该路径。

这与既定契约冲突：现存符号链接不参与真实对象来源判断，应由部署收尾的独立链接 diff/merge 统一移除或替换。

闭合要求：当目录中的每个实体都是符号链接或已记录的旧 import 工作副本时，允许安全转换；仅含未知空目录的情况仍应保持保守，除非旧清单中的后代归属能够证明它是中断残留。

### Medium — 构建元数据未完整保留后代路径和临时命名空间

`ProjectionManifestHelper.IsReservedProjectionPath` 对两份正式清单和 `allowed_symlinks.txt` 只拒绝完全相同的目标路径，没有像 `natives` 一样拒绝其后代。来源若声明 `trident.import.json/child`，规划阶段可以接受，执行后会让正式清单路径成为目录，随后清单写入和读取都会失败。

清单原子写使用随机临时文件名，但该临时命名空间也没有纳入投影目标保留规则，不符合设计中“正式记录及其提交用临时路径均为构建元数据保留位置”的约束。

闭合要求：正式元数据文件路径及其不可能成立的后代必须全部拒绝；临时提交使用明确、可验证且同样受保留规则保护的命名空间。

## 后续验收重点

闭合 findings 后除重新构建外，仍需按原 v2 计划使用真实实例验证：

- 在删除、备份、快照还原和清单提交各阶段制造中断，确认重新规划能够收敛且不丢失既有归属证据。
- 验证整合包更新前后的 persist Delete-Create 回迁仍成立，并验证失败回滚不改变清单。
- 验证 import 文件／目录双向转换、旧链接占位、未知空目录及受管空目录残留的边界。
- 验证所有正式与临时构建元数据路径都不能被 import、persist 或 package 投影占用。
