# 部署优先级与离线仲裁

## 解析与仲裁

`PackageResolver` 负责仓库解析，`SyncPackagesStage` 按 `(repository, namespace, project, source)` 匹配已锁定包。不同来源的同一项目分别保留版本，优先级变化不会令浮动包重新解析。

`PackagePlanner` 不依赖仓库服务。它消费 `LockData.Packages` 及锁内的来源优先级，返回生效包，不修改锁。需要展示仲裁结果的消费方使用同一个 Planner，不读取持久化的胜负标记。

## 来源顺序

按 `(Tier, Index)` 比较，较大的值优先：

| 来源 | Tier | Index |
| --- | --- | --- |
| 手动添加，Source 为 null | 3 | 0 |
| 列入 PackageSourceOrders | 2 | 列表位置，末项最高 |
| 未列出的其他来源 | 1 | 0 |
| 当前整合包来源 PackageSource | 0 | 0 |

先进行两遍仲裁，再排除规则要求跳过的胜者；跳过高优先级来源不会自动启用低优先级来源：

1. 按项目身份选择唯一生效来源。
2. 在项目胜者中按 `RelativeTarget()` 选择唯一目标文件。

同档平局抛出 `PackageConflictException`，不通过字符串顺序替用户选择。项目胜者在目标路径仲裁中落败时，不自动补上该项目的次优来源。

## 文件投影

包仲裁之后，`SourceProjectionPlanner` 扫描整合包源（`import/`）和本地保留（`persist/`），`ProjectionArbitrator` 统一完成路径优先级与祖先／后代遮蔽仲裁：本地保留优先于整合包源，整合包源优先于包。`DeploymentPlanner` 将仲裁结果组装为目标视图，`DeploymentDiffer` 再将它与当前运行目录和旧投影清单比较，产出下载与可执行差异。

- 生效包从共享缓存创建符号链接。
- 整合包源生成真实工作副本，仅在缺失时复制，保留运行中的修改。
- 本地保留生成符号链接。游戏把链接替换成普通文件时，计划先把文件移回本地保留，再恢复链接。
- 本地保留目录中的 `.keep` 将该目录作为整体投影，其子路径不会同时生成较低优先级的投影。
- 整合包源与本地保留不能含符号链接，`.keep` 目录内部也在目标规划时检查。运行目录中的符号链接不参与真实对象归属仲裁，可直接删除并按最终目标重建。
- 空目录以及只含符号链接和其他空目录的目录可由目标对象直接覆盖；包含未知普通文件的目录仍明确报冲突。
- 不再需要的运行目录链接由 `DeploymentDiffer` 产出显式删除操作，扫描不进入链接指向的目录。

被上层投影覆盖的包文件不产生下载需求。计划保留操作顺序，下载完成后再执行投影操作。

## 有效性与定位

`LockValidationHelper.PackagesInput` 包含包声明、启用规则和来源顺序。配置变化使旧锁对当前需求失效；下一次部署重新组装锁内规则和来源信息，再由离线 Planner 仲裁。

| 现象 | 检查位置 |
| --- | --- |
| 同项目多来源丢失版本 | `SyncPackagesStage.MatchKey` 是否保留 Source |
| 来源胜负错误 | `PackagePlanner.RankOf` |
| 不同包撞同一目标 | `PackagePlanner` 的第二遍仲裁 |
| 锁里有包但没有落盘 | 包仲裁结果、跳过规则及投影优先级 |
| 修改来源顺序导致版本漂移 | 解析失效是否错误依赖优先级 |
| 文件存在却仍计划链接 | `DeploymentFileHelper.LinkMatches` 和 `DeploymentDiffer` 的当前视图判断 |
| 导出或单包物化路径不一致 | `PackagePathHelper.RelativeTarget` |
