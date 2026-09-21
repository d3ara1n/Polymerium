# 部署投影归属与差异执行

## 最终状态

部署投影采用目标视图、差异计算和顺序执行三层结构：

```text
DeploymentPlanner.CreateTarget
    -> DeploymentTarget
    -> DeploymentDiffer.Diff
    -> DeploymentPlan
    -> ExecuteDeploymentStage
```

`DeploymentPlanner` 只读取锁定数据以及 package、import、persist 来源，完成目标来源优先级和祖先／后代遮蔽仲裁。它不读取当前 Run Directory 或旧投影清单。

`DeploymentDiffer` 读取当前 Run Directory 与旧 import、persist 清单，将目标视图转换为完整操作列表。真实文件归属、persist mtime 仲裁、链接删除与创建、清单结果都在此确定。执行阶段不重新生成另一份目标视图。

`ExecuteDeploymentStage` 下载所需文件并顺序实施操作，只复核文件系统物理前置条件。删除、迁移或回迁文件后，由执行层裁剪可证明为空的父目录。

## 投影与归属

- package 从共享缓存链接到 Run Directory。
- import 作为真实工作副本复制到 Run Directory，只补缺失，不自动覆盖运行修改。
- persist 单文件通过符号链接投影；游戏以 Delete-Create 替换链接时，旧 persist 清单的 mtime 基线用于决定回迁或由 persist 胜出。
- persist 中含 `.keep` 的目录作为整体链接投影，并可复用旧单文件 mtime 历史完成单文件到目录投影的转换。
- import 清单记录上次成功部署实际生效的工作副本路径；persist 清单记录上次成功部署的单文件路径及源 mtime。

符号链接只表达投影关系，不参与真实对象来源冲突。差异计算会保留正确链接、删除错误或多余链接，并创建缺失链接。空目录以及只含符号链接和其他空目录的目录不是有归属的数据实体，可以由最终文件或链接直接覆盖；包含未知普通文件的目录始终报冲突。

## 生命周期

正式清单在实体操作、链接操作和部署收尾全部成功后提交。清单文件、其不可能成立的后代、`allowed_symlinks.txt`、`natives/` 和专用临时提交目录均为保留路径。

整合包更新成功后只删除已经 stale 的 import 清单，persist mtime 历史继续保留；更新失败回滚不改变两份清单。

快照保存两份投影清单。还原时先验证目标清单，以当前和目标归属的并集对账 import 与 persist 路径，普通内容完成后最后替换或删除正式清单。没有投影清单的旧快照从其 Run Directory 引用推导 import 工作副本范围。

## 验证

完整解决方案构建通过，CLI 与 Polymerium 使用相同的 Planner、Differ 和执行管线。真实实例仍需覆盖 import 文件／目录转换、persist Open-Overwrite 与 Delete-Create、`.keep` 接管、整合包更新成功与回滚、快照还原以及链接全量重建。
