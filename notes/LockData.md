# LockData 与离线部署规划

`data.lock.json` 固定实例的解析需求，不证明文件已经部署完成。`LockData.FORMAT` 为 9，参与 Artifact 区域的输入指纹。

## 锁的内容

- `Platform` 保存包解析使用的 Minecraft 版本和加载器。
- `Vanilla`、`Loader`、`Launch` 分别保存区域输入指纹与输出。`Artifact` 是最终启动数据。
- `Packages` 保留所有来源的完整解析结果与规则结果。包版本不会因为优先级变化而重新解析。
- `PackagesInput` 描述启用的包、规则和来源顺序；`PackageSource`、`PackageSourceOrders` 供离线仲裁使用。
- `RuntimeMajor` 是可选的 Mojang 运行时部署需求，来自全部 Patch 应用后的兼容大版本集合，与用户 Java 偏好无关。没有交集时为 null，部署跳过运行时准备。
- `RuntimeIndex` 可选地保存运行时索引 URL 与哈希。已有索引但旧锁未记录引用时继续离线使用；有哈希时读取前校验，没有哈希时按存在性读取，再验证索引结构。

## 管线

```text
LoadLock
  → InstallVanilla
  → ProcessLoader
  → ApplyLaunchPatch
  → SyncPackages
  → SelectRuntime
  → PersistLock
  → PlanDeployment
  → ExecuteDeployment
```

`BaseLock` 是磁盘锁的只读参照，`Lock` 是各阶段逐步组装的本次需求。`PersistLock` 在文件准备前保存需求，因此部署失败后仍可使用已固定的解析结果。

`LockValidationHelper` 提供部署区域和状态检查共用的指纹计算。`ValidateAsync` 检查区域链、包输入和运行时需求；索引及资源文件是否存在属于 Planner 的职责。旧区域指纹不匹配时重新解析区域，包版本仍按现有匹配规则复用。

## 离线规划

`DeploymentPlanner.CreateTarget(key, data)` 调用离线 `PackagePlanner` 仲裁包，并从包、整合包源（`import/`）和本地保留（`persist/`）生成完整目标视图；它不读取当前运行目录（`build/`）或旧投影清单。`DeploymentDiffer.Diff(key, target)` 再以当前目录和旧清单仲裁真实对象归属，输出下载、复制、移动、链接、目录及清单提交操作。已经就绪的对象不进入操作清单。

`AssetPlanner` 消费本地 `AssetIndex`，`RuntimePlanner` 消费本地 `RuntimeIndex`，两者不联网。索引读取由 `DeploymentIndexHelper` 提供，获取由 `DeploymentIndexService` 提供：部署消费方发现缺失便补齐索引继续规划，状态检查消费方直接返回未就绪。

运行时以 major 共享，索引位于 `cache/runtimes/{major}.json`，文件位于 `cache/runtimes/{major}/`。索引与锁内引用匹配时离线复用；缺失或失效时重新查询目录并记录当前索引引用。实例不固定 Java 补丁版本；删除共享索引后，下次部署获取当前索引并修复文件。用户 Java 偏好只在启动时参与选择，未命中时使用锁定 major，没有可用 Java 则在启动时报错。

## 执行与收尾

`ExecuteDeploymentStage` 先下载并校验文件，再按顺序执行 `DeploymentDiffer` 产出的本地操作。符号链接创建、替换与清理已经包含在同一份差异结果中；执行阶段只复核物理前置条件，并在删除或迁移文件后裁剪空目录。

原生库不参与首页就绪判断。文件操作完成后，`NativeHelper` 直接读取本地原生库归档，按排除规则和覆盖顺序推导预期输出，对照 `build/natives/` 并在必要时通过临时目录替换。没有单独持久化的原生库提取索引。

## Polymerium 消费

`InstanceStateService.RetrieveDeploymentStateAsync` 是资源就绪状态的入口。锁无效则返回需要部署；锁有效且已有本会话结果则复用；没有缓存时执行只读规划。索引本地可读且计划没有下载或本地操作时显示资源已就绪；仅有本地操作时也显示需要部署；已知需要下载时显示需要下载。三个状态共用资源类别的就绪检查表，缺失索引归入对应的资产或运行时类别，无法离线判定具体需求时不猜测类别。

缓存通过已有实例活动和 Profile 事件失效，不监控用户在应用外修改文件。应用重启后重新检查。账号和用户 Java vault 不属于这个状态的输入，资源就绪也不保证启动条件全部满足。

## 故障定位

| 现象 | 检查位置 |
| --- | --- |
| 浮动包版本意外变化 | `SyncPackagesStage` 的身份匹配和平台变化判断 |
| Patch 修改后仍复用旧区域 | `LockValidationHelper` 对应区域的输入指纹 |
| 资源齐全仍计划下载 | `DeploymentFileHelper` 的路径和哈希检查 |
| 链接丢失或重复创建 | `DeploymentDiffer` 的链接差异 |
| 索引缺失时检查联网 | 状态消费方是否误调用 `DeploymentIndexService` |
| 用户改 Java 偏好触发资源重建 | 部署是否错误地重新依赖 vault |
| 原生库不完整 | `NativeHelper` 的归档推导和目录替换 |
