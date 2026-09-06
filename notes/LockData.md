# 启动定义与部署快照：影响面与故障定位地图

本文面向启动与部署故障定位。`profile.json` 和 `launch/` 保存持久意图，`data.lock.json` 保存可丢弃的计算结果。

关联：[POLY-116](https://d3ara1n.atlassian.net/browse/POLY-116)、[POLY-165](https://d3ara1n.atlassian.net/browse/POLY-165)。原生文件格式见 [启动定义](../submodules/Trident.Net/docs/Launching.zh.md)。

## 数据与所有权

```text
实例/
├── profile.json
├── launch/
│   ├── import/
│   │   ├── definition.json
│   │   ├── components/*.json
│   │   └── files/
│   └── user/
│       ├── definition.json
│       ├── components/*.json
│       └── files/
├── data.lock.json
├── build/
│   └── natives/
├── import/
└── persist/
```

- `Profile` 保存 Minecraft 版本、模组加载器、包、规则及运行覆盖项；普通实例无需创建启动定义。
- `LaunchDefinition` 是有序的组件选择清单，`LaunchComponent` 是有身份的完整组件声明。文件存在不等于组件启用。
- `user/definition.json` 优先于 `import/definition.json`；都不存在时，以 Profile 的 Minecraft 和模组加载器作为默认选择。
- 同身份组件按 user → import → 平台查找，使用一个完整定义，不继承被替换组件的依赖或内容。
- 整合包更新通过目录级替换管理 `launch/import/`，不替换 `launch/user/`。整合包导出不带用户定义；快照保存整个 `launch/`。
- 运行目录（`build/`）可以包含用户数据；重置会永久清空它。只有已经放入本地保留（`persist/`）的数据保证跨重置保留。

## 固定部署管线

每个阶段都执行，各自在内部决定复用或重建。

```text
LoadLock          读 BaseLock，按 Profile 创建本次 Lock
ResolveComponents 按选择顺序解析完整组件与依赖，合并 Java 主版本约束
EnsureRuntime     确定实际 Java 主版本和架构，必要时准备内置运行时清单
CompileLaunch     针对 OS、OS 版本、Java 主版本和架构编译或整体复用启动结果
SyncPackages      按包引用、平台和规则计算包锁定状态
FlattenPackages   按来源优先级仲裁包与落盘路径
PersistLock       写回 data.lock.json
GenerateManifest  由启动结果、包、运行时和本地层生成文件清单
SolidifyManifest  下载、校验、投影并解压显式声明的 native
```

`BaseLock` 是上次快照的只读参照，`Lock` 是本次结果。`CompiledLaunch` 是不可变产物，缓存命中时整体复用，不能作为下一轮组件输入。

## 解析与编译约束

1. **顺序由清单决定。** 保持显式选择的顺序，缺少的依赖插入到第一个依赖者之前，不对现有组件拓扑重排。版本要求冲突、依赖被禁用或组件互斥会终止解析。
2. **Java 约束求交集。** 先得到兼容主版本集合，再探测配置的 Java home 或内置运行时的真实版本和架构。原生库的目标架构跟随 JVM，不直接拿宿主架构代替。
3. **文件身份与用途分离。** 完整 artifact 身份包含版本与 classifier；下载必需文件、classpath、客户端 JAR、native 和 Java agent 分别声明用途。Forge 安装依赖不参与 classpath 版本竞争。
4. **classpath 和 native 独立仲裁。** 相同无版本槽位的后声明替换前声明并保留位置，不能用下载集合代替最终 classpath 或解压集合。
5. **参数是声明，不是累积副作用。** Replace 替换已有参数，Append 按序追加；规则按目标求值。编译后的参数保留 artifact 身份引用，启动装配时才绑定物理路径。
6. **本地文件按所有权解析。** 相对路径相对于组件文件；导入组件的引用不能越出其 `launch/import/`，引用路径中的符号链接会被拒绝。用户组件允许外部本地文件，解析时验证并计算内容散列。
7. **native 保持实例隔离。** 显式 native 解压到该实例的 `build/natives`；普通 JAR 即使名字含 `natives` 也不会被推断为解压项。游戏自行解压仍可写入同一目录，因此不能整体清空它。当前选中归档的条目直接覆盖，ZIP 时间戳不代表库版本，未声明的文件不删除。

## 缓存边界

- `LaunchDefinitionSnapshot` 对 import/user 两棵树的路径与文件内容计算指纹，未启用文件变化也可能触发重新编译。
- 本地组件文件都进行语法校验；只有选中组件及其依赖闭包会解析本地引用并参与启动。
- 解析指纹包含定义树指纹以及实际解析出的组件内容和来源；外部用户文件的内容散列进入解析结果。
- 编译指纹包含编译器版本、解析指纹和完整目标。Java 架构、主版本或 OS 信息改变会使结果失效。
- 组件解析仍然执行，平台元数据访问依赖服务自身缓存；编译结果复用不等于整条部署管线无需网络。
- 损坏、缺失或无法复用的 lock 从持久意图重新计算，不做旧结构迁移。完整性检查不读取 BaseLock。
- `PersistLock` 位于文件落实之前；存在 lock 不表示下载与解压已成功完成。

## 责任域地图

路径相对于 `submodules/Trident.Net/src/`，宿主文件另行标注。

| 责任域 | 文件 | 首查问题 |
| --- | --- | --- |
| 原生模型 | `TridentCore.Abstractions/Launching/` | 定义形态、参数、规则、文件用途与编译产物 |
| 锁定数据 | `TridentCore.Abstractions/FileModels/LockData.cs` | 序列化、平台、Java 运行时指纹、包锁定数据 |
| 文件读取 | `TridentCore.Core/Utilities/LaunchDefinitionSnapshot.cs` | 文件优先级、路径约束、本地内容变化 |
| 组件解析 | `TridentCore.Core/Services/LaunchDefinitionResolverService.cs` | 整体替换、显式顺序、依赖版本、Java 交集 |
| 平台元数据 | `TridentCore.Core/Services/PlatformComponentService.cs`、`Utilities/MetadataComponentHelper.cs` | 原版与加载器声明、native classifier、外部字段转换 |
| Java 选择 | `TridentCore.Core/Utilities/JavaHelper.cs`、`Engines/Deploying/Stages/EnsureRuntimeStage.cs` | 配置路径无效、版本或架构不匹配、运行时目录 |
| 启动编译 | `TridentCore.Core/Services/LaunchCompilerService.cs`、`Engines/Deploying/Stages/CompileLaunchStage.cs` | 参数、主类、classpath、native 仲裁、缓存复用 |
| 文件清单 | `TridentCore.Core/Engines/Deploying/Stages/GenerateManifestStage.cs` | 漏文件、错误 native、包落点、本地资产索引 |
| 文件落实 | `TridentCore.Core/Engines/Deploying/Stages/SolidifyManifestStage.cs` | 下载散列、投影、符号链接、解压排除项 |
| 路径与装配 | `TridentCore.Abstractions/Utilities/ArtifactHelper.cs`、`TridentCore.Core/Extensions/CompiledLaunchExtensions.cs` | Maven 坐标、共享库路径、本地文件、参数引用绑定 |
| 导入导出 | `TridentCore.Core/Importers/`、`Exporters/`、`Utilities/MetadataExportHelper.cs` | 格式转换、不可表达的启动声明、原生文件随包携带 |
| 整合包提交 | `TridentCore.Core/Services/InstanceModpackService.cs`、`Facilities/FileTransaction.cs` | 暂存验证、文件提交与回滚、恢复材料保留 |
| 配置发布 | `TridentCore.Core/Services/ProfileManager.cs` | 文件提交后替换缓存、旧引用失效、通知快照隔离 |
| 诊断出口 | `TridentCore.Core/Services/ImporterAgent.cs`、`Services/ExporterAgent.cs` | 转换异常与诊断日志 |
| 宿主提示 | `src/Polymerium.Avalonia/PageModels/NewInstancePageModel.cs`、`Properties/Resources*.resx` | 导入诊断、部署阶段本地化 |

## 整合包事务与恢复

- 安装先暂存完整实例，再发布目录与 Profile；更新将整合包源、导入启动定义、受影响的工作副本文件、实例附件、锁定数据失效与 Profile 作为同一次事务提交。
- 更新工作目录为实例内的 `.update/`，安装工作目录为实例根目录下的 `.install-{key}/`，包含 `staged/`、`backup/` 与 `journal.json`。
- 提交失败按实际完成的文件操作回滚；回滚失败保留恢复材料，阻止后续实例活动和 Profile 写入。最终态日志有效时，单纯清理失败不阻止正常使用，后续事务重试清理。
- 不提供自动崩溃恢复。人工处理前退出所有客户端并复制实例及事务工作目录；重命名可能已完成而日志尚未更新，必须结合实际文件判断，不能直接删除备份或重试。

## 包锁定与规则

包解析独立于启动组件编译，排查时不要把包来源覆盖和启动组件替换混为一谈。

- `SyncPackagesStage` 按项目身份与 `Source` 区分来源，固定版本变化需要重新解析，浮动版本的平台失效依据 Profile。
- `LockedPackage.Resolved` 保存完整 `Package`；规则变化直接以此重新求值，不制造占位包，也不为规则重算额外请求仓库。
- `FlattenPackagesStage` 按项目身份和目标路径做两轮来源仲裁，败者保留锁定事实并记录 `SuppressedBy`，不加入文件清单；同优先级冲突报错。
- `PackagePlanner.PlanAsync` 供导出器和宿主使用，与部署阶段共享解析和规则求值逻辑。
- 具体来源顺序与覆盖规则见 [DeploymentPriority.md](DeploymentPriority.md)。

## 故障定位

| 症状 | 检查方向 |
| --- | --- |
| 改了 Profile 版本后失败或启动不符合预期 | 选择是绑定 Profile 还是固定版本；同身份本地组件是否匹配当前选择 |
| 重复部署后参数翻倍 | `CompileLaunchStage` 是否整体复用结果，调用方是否又把结果作为输入 |
| Forge 缺安装依赖或 ASM 冲突 | `Required` 文件是否仍存在，最终 classpath 是否只包含选中的版本 |
| native 下载 404 或架构错误 | 元数据 classifier、规则与 `CompiledLaunch.Target` 中的 JVM 架构 |
| 切换 native 版本却加载旧内容 | 当前解压条目与用途是否正确；不能仅凭 ZIP 时间戳判定内容一致 |
| 本地库变动后仍复用旧结果 | 活动组件的本地内容散列及解析指纹 |
| 导出不受支持 | 目标格式是否能承载导入启动定义，或是否存在无法无损表达的参数、规则、native 或校验语义 |
| 导入后缺自定义组件 | MMC 清单是否选中该组件，不能用孤立 patch 文件自动激活组件 |
| 启动前报告目标变化 | Java home 的实际主版本/架构或 OS 信息与锁定目标不一致，需要重新部署 |
| mod 版本或规则不符 | `SyncPackagesStage` 的匹配与失效条件，以及 `PackagePlanner.EvaluateRule` |

## 验证与维护

运行 `dotnet test submodules/Trident.Net/tests/TridentCore.Tests/TridentCore.Tests.csproj` 和 `dotnet build Polymerium.slnx`。测试仅使用项目内沙箱与离线元数据，不启动 Minecraft；元数据夹具不能替代真实整合包的部署启动验收。

变更定义、编译、缓存、目录所有权或部署顺序时同步更新本文件；变更包来源仲裁时同步更新对应专题。格式转换无法保留执行语义时明确拒绝，不能静默输出行为不同的整合包。
