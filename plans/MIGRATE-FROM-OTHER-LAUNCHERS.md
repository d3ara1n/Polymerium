# 从其他启动器迁移实例（Migrate）

## 做什么

在 设置 → Maintenance → Tools 增加入口「Migrate from another launcher」，点击弹出迁移 Modal。Modal 内：选择启动器类型、填写（或自动探测）数据目录 → Scan 就地扫描出可导入实例列表 → 勾选后 Import 开始异步迁移。

与现有 import/install 的区别：那两者由 Trident 提供、操作 zip 整合包；本功能是 Polymerium 定制能力，操作的是其他启动器的**数据目录**，Trident 只提供仓库反查与实例注册等底层 API。

迁移语义：**这是迁移，不是导入**。源游戏目录的文件经启动器 adapter 声明的黑名单过滤后原样拷入新实例的 `build/`（运行目录），其中能被资源仓库识别的包文件（mods/resourcepacks/shaderpacks 等目录下的内容）在拷贝时剔除、转为 `profile.json` 中锁定版本的 pref 包条目。实例没有上游整合包，不存在 `import/` 层的意义，不创建。未识别的文件不特殊处理，就是普通文件留在 `build/`。

黑名单的必要性：Trident 部署只把包 symlink 进 `build/`，assets/libraries/运行时全在共享缓存物化、启动参数指向共享缓存，游戏永远不读 `build/` 下的 `versions/`、`libraries/`、`assets/`、`runtime/` 等启动器托管路径，原样拷入是纯死重（数 GB，拷贝耗时翻倍，reset 时陪葬）。各启动器的黑名单一次做不全是既定事实——**先把黑名单机制（基础设施）实现好，各启动器的具体规则留作后续讨论**。

首版只支持**官方启动器**（整个 `.minecraft` 视为单实例），结构上为 HMCL / PrismLauncher·MultiMC 留 adapter 扩展点，后续再加。PCL 明确不做：它的 json 是手拼字符串，格式时好时坏、字段时有时无，反序列化兼容是无底洞。

## 想要什么效果

- 分钟级的仓库识别**不阻塞 app**：点 Import 后 Modal 即关闭，每个选中实例一条独立后台任务，进度挂在 NotificationService 的 `PopProgress` Growl 上（导出整合包同款：通知生命周期 = 任务生命周期，进度对象自持，不需要任何外部任务状态持有者）。完成 Growl 带 Open 按钮跳实例页；失败用 Danger 级通知常驻。
- Modal 内「填写表单 ↔ 展示扫描结果」用**结果对象存在性**驱动：一个可空结果属性 + `PlaceholderContainer`（无结果显示表单占位，有结果显示结果），撤销结果 = 赋 null 回到表单重新扫描。不引入 Step enum。（AGENTS.md 的 View State Representation 已收录此第三层。）
- 批量迁移多个实例时，**相同内容的文件只识别一次**（按内容 hash 共享识别池，同 hash 在批量请求中只占一个槽位），同一文件在不同实例得到相同的锁定 pref 条目。
- 迁移完成的实例：MC 版本/loader 正确；识别的包在实例页作为受管包出现（可禁用、可更新、可随部署物化）；未识别文件原样留在运行目录，游戏正常加载。
- 选 Modal 而非 Dialog/Page：Dialog 语义是「拿一个决定」，承载不了就地扫描 + 表单 + 列表；Page 约定展示持久化内容，迁移是一次性流程，状态离开即死，不该发明一次性状态持有者。交互态归 Modal（关掉即弃，扫描秒级可重来），任务态归通知。Tools 区入口用 `PopModal`，与 Garbage Collect 同款。

## 调研得知的注意事项（已核实）

- **deploy 不删 `build/` 普通文件**：`SolidifyManifestStage` + `SymlinkPhotos.Apply` 只管理 symlink（多余的删、目标变的换），PersistentFile 投影「无则复制、有则不管」。拷入 `build/` 的未识别文件、存档、配置在后续 deploy/launch 中安全。
- **硬约束**：普通文件占住包 symlink 的目标路径会抛 `BuildArtifactConflictException` 导致 deploy 失败。因此已识别文件必须按识别集合剔除——**不看文件名**：即使仓库文件名与本地不同也要剔除本地文件，否则同一 mod 会以两个文件名共存、被加载两次。
- **reset 边界**：reset 清空整个 `build/`，迁移实例的未识别文件与存档只存在于 `build/`，reset 后不可恢复。这是 reset 既有语义（GLOSSARY 已要求明示数据丢失边界），接受，不额外处理。
- **存量 bug，须先修**：`RepositoryAgent.IdentityAsync(ReadOnlyMemory<byte>)` 的跨仓库 fallback 是死代码——非 async 方法里 try/catch 包住 `return repository.IdentifyAsync(...)`，未命中异常在 Task 里异步 fault，同步 catch 捕不到，第一轮 return 即结束；Labels 顺序为 curseforge → modrinth → favorite，实际只查 CurseForge，Modrinth 从未被查询（`AssetImporterDialog` 的现有调用同样受此限）。修复方向：改为 async 顺序 fallback，命中即返回，全部 `ResourceNotFoundException` 才算未识别。两个配套修正：`ModrinthRepository.IdentifyAsync` 未命中抛的是 Refit `ApiException`(404)，须统一表达为 `ResourceNotFoundException`；`FavoriteRepository` 面向用户、保持非 Hidden，其 `IdentifyAsync` 的 `NotImplementedException` 须改为 `NotSupportedException`（与 Packwiz 一致）——agent 循环遇 `NotSupportedException` 跳过该仓库，遇 `ResourceNotFoundException` 继续下一个。
- **批量识别可实现，首版即采用**：新增 `RepositoryAgent.IdentifyBatchAsync` 替代逐文件调用。CurseForge 侧模型现成（`POST /v1/fingerprints/{gameId}` 收指纹数组，响应含 `ExactMatches` + `UnmatchedFingerprints`）；Modrinth 侧需给 `IModrinthClient` 加一个方法：`POST /v3/version_files`，请求 `{hashes: [...], algorithm: "sha1"}`，响应为 hash→version 字典、**未命中 hash 直接缺席**（无公开数量上限，ratelimit 300 req/min）。fallback 在批量层面实现：CurseForge 未命中集合再喂 Modrinth，最终缺席即未识别。project/作者等元数据用现成批量端点补齐（CurseForge `POST /v1/mods`、Modrinth `GET /v3/projects`）。批量请求本身失败（网络/限流 429）= 任务失败进 Danger 通知，**不**静默降级为全未识别；单文件不再有独立失败路径。
- 识别结果写成 pinned pref（`pref://<repo>/<identity>@<version>` 锁定版本，不漂移）；包条目 `Source = null`，在 `FlattenPackagesStage` 里属 manual 层（优先级最高），不会被任何来源压制。
- 首次 deploy 需要网络（解析下载 URL + 下载包文件）。已识别文件字节与仓库一致（否则识别不上），理论上可预填共享缓存避免重下——列为后续优化，不阻塞首版。
- 官方启动器元数据：`launcher_profiles.json` + `versions/<id>/<id>.json` 继承链探测 MC 版本与 loader；探测失败按 vanilla 处理并在列表行标警告，用户事后可在实例设置里改。多 profile 共享同一 `.minecraft` 时取哪条的版本，与黑名单同属启动器规则，留待后续讨论。
- 识别池：键为文件内容 hash（SHA1 + CurseForge fingerprint 一对），去重后喂批量识别；可取消。单实例任务管线为 列文件清单（不算 hash，保证 Scan 快）→ 算 hash 并批量识别（每仓库一次请求，秒级）→ 拷贝（剔除已识别，进度 n/total 挂在此阶段）→ 注册 profile。
- 中途取消/失败：实例目录由任务创建，取消即整目录删除，无残留；`ProfileManager.RequestKey` 处理重名、`ProfileManager.Add` 放最后一步。进程被杀的极端情况会留未注册孤儿目录（实例列表不可见），可接受，交由后续存储清理。
- 入口与所有新文案需三文件同步：`Resources.resx` / `Resources.zh-hans.resx` / `Resources.Designer.cs`。
