# 部署投影 v2 人工 GUI 验收

立案日期：2026-09-21

## 背景

POLY-179 与 GitHub #93 已完成部署投影机制重构：整合包源（Pack Source，`import/`）继续以真实工作副本进入运行目录（Run Directory，`build/`），本地保留（Local Data，`persist/`）与包继续通过符号链接投影；Core 新增归属清单、目标视图、差异计算和顺序执行，以恢复源删除跟随并统一文件、目录和链接转换。

完整解决方案构建只能证明代码能够编译，不能证明真实文件系统、游戏写入方式、跨平台符号链接、整合包更新和快照生命周期能够按设计收敛。本待办提供可以直接执行的人工操作流程及逐步预期行为。

## 发版门槛

- 使用真实整合包、真实包缓存和实际游戏运行，不以构造对象、断言或仅检查构建结果替代。
- Polymerium 中存在对应入口的操作必须从 GUI 发起；文件管理器、编辑器、游戏和系统进程管理器只用于准备输入、制造真实写入形态、中断进程和检查结果。
- 使用一次性验证实例。重置、来源接管和中断流程可能有意删除 Run Directory 内容，不得使用含唯一存档或其他不可替代数据的实例。
- 主开发平台执行全部流程；`win-x64`、`linux-x64`、`osx-arm64` 的实际发布产物分别执行最后的平台冒烟流程。
- 任一步骤出现数据丢失、未知文件误删、错误覆盖、无法重试收敛、界面状态错误或游戏读取失败，即停止发版并保留本 todo。
- 全部可执行步骤符合预期后才允许回填、归档并发版。

## 固定验证数据

在执行流程前准备一个可启动的真实实例，并记录实例 key、Minecraft 版本、模组加载器、Java 运行时和安装包列表。下文以实例目录中的物理路径简称 `import/`、`persist/`、`build/`。

在应用关闭时准备以下 Pack Source 文件：

```text
import/config/polymerium-projection/workspace.txt       = IMPORT-WORKSPACE-V1
import/config/polymerium-projection/delete-sync.txt     = IMPORT-DELETE-SYNC-V1
import/config/polymerium-projection/delete-deploy.txt   = IMPORT-DELETE-DEPLOY-V1
import/projection-shape/item/child.txt                  = IMPORT-DIRECTORY-V1
```

再准备一个能在游戏内直接观察的真实目录资源包：

```text
import/resourcepacks/PolymeriumProjectionValidation/
```

资源包必须至少改变一个语言文本或贴图，并包含可以在游戏中确认的资源。不要用空目录或伪造资源替代。

在 `persist/` 中准备：

```text
persist/config/polymerium-projection/persist.txt        = PERSIST-V1
```

选择一个已安装包，记录其 Run Directory 相对目标和缓存对象路径，后文称其为 `<package-target>` 与 `<package-source>`。该包必须是可以安全复制的真实文件。

## 流程一：首次部署与无变化重复部署

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 启动 Polymerium，打开验证实例主页并刷新状态。 | 实例显示需要部署；若缓存不完整，同时显示对应下载类别，而不是错误显示已就绪。 |
| 2 | 从 GUI 执行部署，持续观察活动阶段和文件计数。 | 依次出现规划部署和应用部署；应用阶段的文件计数同时覆盖下载与本地文件操作，进度不会倒退或溢出。 |
| 3 | 部署完成后检查 `build/`。 | 四个 Pack Source 测试文件和目录资源包内容是普通文件；`persist.txt` 是指向 `persist/` 对应文件的文件链接；已安装包是指向缓存对象的文件链接。 |
| 4 | 从 GUI 开始游戏并在游戏内启用验证资源包。 | 游戏正常启动，资源包能被枚举，预设语言文本或贴图实际生效。 |
| 5 | 退出游戏，不修改任何文件，再次从 GUI 执行部署。 | 不重新下载完整资源，不覆盖 Pack Source 工作副本，不改变正确链接目标；部署结束后实例显示已就绪。 |

## 流程二：Workspace 同步与还原

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 在应用外把 `build/config/polymerium-projection/workspace.txt` 改成 `RUN-WORKSPACE-V2`，随后在 GUI 打开 Workspace 并刷新。 | Workspace 显示该文件的工作副本比 Pack Source 更新，并允许查看差异、Sync to Import 和还原工作副本。 |
| 2 | 在 GUI 对该条目执行 Sync to Import。 | `import/.../workspace.txt` 变成 `RUN-WORKSPACE-V2`；`build/.../workspace.txt` 保持相同内容；条目不再显示内容差异，版本控制状态同步刷新。 |
| 3 | 再把 Run Directory 文件改成 `RUN-WORKSPACE-V3`，刷新 Workspace 后执行还原工作副本。 | Run Directory 文件恢复成 `RUN-WORKSPACE-V2`；Pack Source 不变；同目录其他文件不受影响。 |
| 4 | 在应用外删除 `import/.../delete-sync.txt`，刷新 Workspace。 | 仍存在的 Run Directory 工作副本显示为 Pack Source 已删除，而不是从列表消失或被当成未知文件。 |
| 5 | 对该条目执行 Sync to Import。 | Pack Source 文件以 Run Directory 内容 `IMPORT-DELETE-SYNC-V1` 重新创建；工作副本不被删除。 |
| 6 | 再次删除该 Pack Source 文件，刷新 Workspace 后执行还原工作副本。 | 对应 Run Directory 工作副本被删除；Pack Source 仍保持不存在；其他工作副本不受影响。 |

## 流程三：Pack Source 删除跟随与未知文件保护

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 确认 `delete-deploy.txt` 已完成部署，然后在应用关闭时创建 `build/config/polymerium-projection/unmanaged.txt = KEEP-UNMANAGED`。 | Run Directory 同时存在受管工作副本和从未进入清单的未知保护文件。 |
| 2 | 删除 `import/.../delete-deploy.txt`，启动 Polymerium 并从 GUI 执行部署。 | 原 `build/.../delete-deploy.txt` 被删除；`unmanaged.txt` 仍存在且内容不变；其他 Pack Source 工作副本不受影响。 |
| 3 | 再次从 GUI 执行部署。 | 不再次尝试删除同一路径，不报孤儿或形态冲突，结果保持稳定。 |
| 4 | 在 `import/` 中重新创建 `delete-deploy.txt = IMPORT-DELETE-DEPLOY-V2` 并部署。 | Run Directory 重新出现普通工作副本，内容为 `IMPORT-DELETE-DEPLOY-V2`；未知保护文件继续保留。 |

## 流程四：包、Pack Source 与 Local Data 优先级

本流程使用预先记录的 `<package-target>`。复制来源时保持文件本身有效，流程完成后恢复原实例状态再启动游戏。

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 在只有包来源的状态下从 GUI 部署。 | `build/<package-target>` 是指向 `<package-source>` 的链接。 |
| 2 | 关闭应用，把 `<package-source>` 复制到 `import/<package-target>`，重新打开并部署。 | Pack Source 接管目标；Run Directory 目标变为普通工作副本，不再是包链接；缓存对象不被删除。 |
| 3 | 关闭应用，把同一有效文件复制到 `persist/<package-target>`，重新打开并部署。 | Local Data 接管目标；Run Directory 目标变为指向 `persist/<package-target>` 的链接；Pack Source 文件保留但不生效。 |
| 4 | 删除 `persist/<package-target>` 并部署。 | Pack Source 重新接管，目标恢复为普通工作副本；不会把旧 Run Directory 内容回迁并覆盖 Pack Source。 |
| 5 | 删除 `import/<package-target>` 并部署。 | 包重新接管，目标恢复为指向 `<package-source>` 的链接。 |
| 6 | 再次部署并检查三处来源。 | 结果稳定，缓存、Pack Source 和 Local Data 中不应被删除的实体均保持原样。 |

## 流程五：Local Data 单文件写入与 Delete-Create

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 从 GUI 部署后，通过 `build/.../persist.txt` 把内容直接改成 `PERSIST-OPEN-V2`。 | 因为目标仍是链接，`persist/.../persist.txt` 立即变成 `PERSIST-OPEN-V2`。 |
| 2 | 从 GUI 再次部署。 | 链接保持不变，内容仍是 `PERSIST-OPEN-V2`，不会产生双副本。 |
| 3 | 关闭应用，删除 Run Directory 中的文件链接，在相同路径创建普通文件 `RUN-DELETE-CREATE-V3`，不要修改 Local Data。 | Run Directory 与 Local Data 暂时形成两个普通副本，Local Data 内容仍为 `PERSIST-OPEN-V2`。 |
| 4 | 启动 Polymerium 并从 GUI 部署。 | `RUN-DELETE-CREATE-V3` 被回迁到 Local Data；Run Directory 恢复为文件链接；通过链接读取到 `RUN-DELETE-CREATE-V3`。 |
| 5 | 再次删除链接并创建普通文件 `RUN-DELETE-CREATE-V4`，同时把 Local Data 改成 `PERSIST-CHANGED-V4`，然后部署。 | 按当前已知策略由发生变化的 Local Data 胜出；Run Directory 普通副本被移除并恢复链接，最终内容为 `PERSIST-CHANGED-V4`。 |
| 6 | 再次部署。 | 不发生来回覆盖，清单修改时间基线已经更新，结果稳定。 |

## 流程六：Local Data `.keep` 目录接管

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 在尚未使用 `persist/saves/.keep` 时启动游戏，在一次性实例中创建并保存 `ProjectionValidationWorld`，然后退出游戏。 | 世界首先只存在于 `build/saves/ProjectionValidationWorld`。 |
| 2 | 关闭应用，创建 `persist/saves/.keep`，启动 Polymerium 并部署。 | 现有世界被迁入 `persist/saves/ProjectionValidationWorld`；`build/saves` 变为指向 `persist/saves` 的目录链接；世界数据不丢失。 |
| 3 | 启动游戏进入该世界，产生一次明确的保存变化后退出。 | 新写入直接落在 `persist/saves`，Run Directory 不产生独立世界副本。 |
| 4 | 关闭应用，删除 `build/saves` 目录链接，创建普通目录 `build/saves` 并写入新文件 `runtime-only.txt = RUNTIME-ONLY`，随后部署。 | 新文件被迁入 `persist/saves/runtime-only.txt`；Run Directory 再次恢复为目录链接；已有世界内容不被覆盖。 |
| 5 | 再次启动世界并退出。 | 世界可正常读取并保存，说明目录接管没有破坏实际游戏数据。 |

## 流程七：文件与目录形态转换及冲突保护

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 确认 `import/projection-shape/item/child.txt` 已部署，然后关闭应用，删除 `import/projection-shape/item/` 目录并创建普通文件 `import/projection-shape/item = IMPORT-FILE-V2`。 | Pack Source 已从目录后代转换为父路径普通文件。 |
| 2 | 启动 Polymerium 并部署。 | 旧受管 `build/.../item/child.txt` 被清理，空目录被裁剪，`build/.../item` 成为内容为 `IMPORT-FILE-V2` 的普通工作副本。 |
| 3 | 关闭应用，删除 Pack Source 普通文件，重新创建 `import/.../item/child.txt = IMPORT-DIRECTORY-V3`，再启动并部署。 | Run Directory 普通文件退出管理并被目录结构替换，`child.txt` 正确物化；重复部署稳定。 |
| 4 | 准备另一个 Pack Source 普通文件目标 `import/projection-shape/blocker = BLOCKER`，同时在首次部署前创建 `build/projection-shape/blocker/unknown.txt = KEEP-BLOCKER`。 | Run Directory 目标被含未知普通文件的目录占用。 |
| 5 | 从 GUI 部署。 | 部署明确报告目标冲突；`unknown.txt` 不被删除或覆盖；Pack Source 源文件不变。 |
| 6 | 删除 `unknown.txt`，保留空目录，再从 GUI 重试部署。 | 空目录可以被安全接管，目标成为 Pack Source 普通工作副本，部署成功。 |

## 流程八：链接完整重建

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 在一个成功部署且应用已关闭的实例中，记录所有包和 Local Data 链接及其目标。 | 获得后续比较基线，不修改链接目标中的实体内容。 |
| 2 | 删除 Run Directory 内全部这些链接，启动 Polymerium 并部署。 | 所有仍需要的链接按完整目标视图恢复；缓存和 Local Data 实体未被删除。 |
| 3 | 关闭应用，把一个链接改到错误目标，并在 Run Directory 增加一个指向安全临时目录的多余链接。 | Run Directory 同时存在错误链接和不再需要的链接。 |
| 4 | 启动 Polymerium 并部署。 | 错误链接被替换为正确目标，多余链接被删除；部署扫描不进入链接目标，临时目录中的实体内容保持不变。 |
| 5 | 再次部署。 | 不再产生链接操作，首页回到已就绪。 |

## 流程九：首页下载分类与实际部署进度

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 关闭应用，分别备份后移除一个库、一个包对象、一个资产对象和一个已锁定 Java 运行时文件。 | 验证实例形成四类真实缓存缺失，不修改 profile 或版本锁。 |
| 2 | 启动 Polymerium 并刷新实例首页。 | 首页将实例判定为需要下载，并正确反映库、包、资产和运行时类别；状态探测本身不联网修复缓存。 |
| 3 | 从 GUI 部署并观察进度。 | 缺失内容在部署流程中下载，随后执行本地投影操作；文件计数和阶段切换一致。 |
| 4 | 部署完成后刷新首页。 | 所有下载类别恢复就绪。 |
| 5 | 只删除一个 Run Directory 链接并刷新首页。 | 首页显示需要部署而不是需要下载。完成部署后链接恢复并重新显示已就绪。 |

## 流程十：真实整合包更新

另选一个在资源仓库中确实存在旧版和新版的整合包作为更新实例，记录两个版本之间可观察的新增、删除和形态变化。

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 通过 GUI 安装旧版整合包、部署并启动一次。 | 旧版 Pack Source、工作副本和 profile 处于可运行状态。 |
| 2 | 修改一个旧版工作副本，并在 Local Data 中准备一个更新后仍应保留的文件。 | 更新前同时存在运行修改与独立持久数据。 |
| 3 | 从 GUI 更新到新版整合包。 | GUI 完成下载、暂存和提交；Pack Source 替换为新版，旧工作副本退出，Local Data 保留，profile 显示新版来源。 |
| 4 | 从 GUI 部署。 | 新增内容出现，已删除内容不残留，文件／目录形态变化成功收敛；persist 修改时间历史继续可用于单文件投影。 |
| 5 | 启动游戏并检查新版可观察内容及 Local Data。 | 新版整合包可运行，更新前准备的 Local Data 仍生效。 |
| 6 | 若更新界面提供取消入口，再对一个未更新实例在下载或暂存阶段取消。 | 取消发生在提交前时，旧 Pack Source、工作副本、profile、附件和 Patch 保持原状；之后重新执行更新可以成功。若当前 GUI 没有可达取消入口，回填时记录为不适用，不用强杀替代此步骤。 |

## 流程十一：新快照保存与恢复

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 完成一次成功部署，在 Pack Source 工作副本、Local Data 单文件、`saves/.keep` 目录和包链接均存在时，从 GUI 创建快照。 | 快照创建成功，统计中包含 Pack Source、Local Data、Patch、受管工作副本和两份投影清单。 |
| 2 | 快照后修改 Pack Source 与工作副本，删除一个原有源文件，新增一个源文件，修改 Local Data，并重新部署。 | 当前实例与快照形成明确差异，仍保持可部署。 |
| 3 | 从 GUI 选择刚创建的快照并执行恢复。 | 恢复前目标 import 与 persist 清单均完成验证；普通内容恢复后，两份清单分别原子替换。 |
| 4 | 检查 profile、Pack Source、Local Data、Patch 和 Run Directory 工作副本。 | 所有内容回到快照代次；快照之后新增的受管工作副本被移除；未知 Run Directory 内容没有被扩大为 import 清理范围。 |
| 5 | 从 GUI 连续部署两次。 | 第一次按恢复后的清单收敛链接和派生内容；第二次无重复修复或冲突。 |
| 6 | 启动游戏。 | 快照中的资源和存档可被实际读取。 |

## 流程十二：旧快照兼容恢复

本流程需要一个机制变更前由正式 Polymerium 版本创建、且不包含投影清单的真实快照。不得手工删除新快照清单伪造旧格式。

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 用旧正式版本创建包含 Pack Source 工作副本和 Local Data 的快照，升级到待发布版本。 | 旧快照在新版本快照列表中可见。 |
| 2 | 改变当前实例内容并部署，然后从 GUI 恢复旧快照。 | 恢复流程从旧快照引用推导 import 工作副本范围，不要求旧快照存在新清单。 |
| 3 | 从 GUI 部署两次并启动游戏。 | 第一次建立新投影清单和链接基线，第二次稳定；旧快照内容可运行，未知 Run Directory 内容未被误删。 |

## 流程十三：实例重置

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 在一次性实例中同时准备 Pack Source、Local Data 和 `build/unprotected-validation.txt = DELETE-ME`。 | 三类数据边界清晰，未保护文件只存在于 Run Directory。 |
| 2 | 从 GUI 打开重置操作并阅读确认信息。 | 警告明确说明 Run Directory 中的存档、截图、日志和配置可能永久删除，不能把重置描述为无害清理。 |
| 3 | 确认重置。 | Run Directory 和部署锁被清除；Pack Source 与 Local Data 保留；`unprotected-validation.txt` 被删除。 |
| 4 | 从 GUI 重新部署并启动游戏。 | 从空 Run Directory 建立新的清单和链接基线，Pack Source 与 Local Data 内容重新生效。 |

## 流程十四：部署中断与重试收敛

为保证本地应用阶段有足够观察时间，使用包含大量真实 Pack Source 小文件和多个包的验证实例。只在一次性实例执行。

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 清除一个较大的缓存对象，从 GUI 开始部署，在下载阶段使用系统进程管理器终止 Polymerium。 | 进程终止不会把临时下载文件当成完成对象，正式实例内容不应被错误提交为已部署。 |
| 2 | 重新启动 Polymerium，从 GUI 再次部署。 | 下载重新验证并完成，部署能够继续，不要求手工删除临时文件。 |
| 3 | 准备 Pack Source 文件／目录转换和链接重建，再开始部署；进入应用部署阶段后终止进程。 | 磁盘可能只完成部分顺序操作，但正式清单不会提前描述尚未完成的最终状态。 |
| 4 | 重新启动并再次部署。 | 已完成的删除、复制或链接操作不会破坏性重复；空目录和中断残留能够继续收敛。 |
| 5 | 为 Local Data Delete-Create 回迁准备大量同阶段操作，并在应用阶段终止一次后重试。 | Local Data 内容不丢失，最终 Run Directory 恢复正确链接，重复部署稳定。 |

## 流程十五：发布平台 GUI 冒烟

在 `win-x64`、`linux-x64` 和 `osx-arm64` 的实际发布产物上分别执行下表。Windows 必须记录开发者模式和符号链接权限，不能用管理员环境掩盖普通用户路径问题。

| 步骤 | 人工操作 | 预期行为 |
| --- | --- | --- |
| 1 | 安装或解压对应平台发布产物，启动 Polymerium，打开同结构的验证实例。 | 应用正常启动，实例主页能够完成状态探测。 |
| 2 | 从 GUI 部署同时含包、Pack Source 和 Local Data 的实例。 | 普通工作副本与文件／目录链接的实体类型和目标均正确，平台路径分隔符不会导致清单读取失败。 |
| 3 | 关闭应用，删除一个包链接和一个 Local Data 链接，再启动并部署。 | 两个链接均被正确重建，不影响源实体。 |
| 4 | 从 GUI 创建快照，修改一个 Pack Source 文件和一个 Local Data 文件，再恢复快照。 | 两类内容和投影清单恢复，随后部署能够收敛。 |
| 5 | 从 GUI 开始游戏并检查验证资源包、一个安装包和 Local Data 内容。 | 游戏成功启动，三类内容都实际生效。 |

## 执行回填

执行过程中可以使用临时记录保存截图、日志和目录树，但不要边执行边把本文改成半计划、半结果。

全部流程执行结束后，按 `plans/README.md` 将本文整体替换为最终验收记录，不保留上述构想式操作正文。回填结果至少包含：

- 验证提交、Polymerium 与 Trident 版本、三个操作系统和实际整合包环境。
- 每个流程实际执行到的步骤、与预期不同的地方、截图或日志证据位置及通过／失败结论。
- 标记为不适用的步骤及其不可达原因。
- 发现的问题、对应修复提交和重新执行后的结果。
- 明确的最终结论：允许发版或阻止发版。

只有所有可执行步骤通过、任何不适用项均有合理且经确认的原因，并且最终结论为允许发版时，才将本文移入 `plans/archived/` 并进入发布流程；否则保持为活动 todo，修复后重新执行受影响流程。
