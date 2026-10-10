# LabyMod loader 支持

追踪：POLY-182（任务）；父级 POLY-181（自建 Launcher Meta 服务 Epic）；前置 POLY-183。

## 背景

Trident 的 loader 元数据（Forge / NeoForge / Fabric / Quilt 的版本清单与版本 JSON）全部经由 PrismLauncher 的 meta 服务解析。这条上游通道不由我们控制，也无法往里添加新 loader——分发渠道不在 Prism meta 里的东西就接不进来（LabyMod 是第一个具体动机；此前预留的 Flint loader 身份也因为这个限制一直没接上）。方向上是自建 launcher meta 服务，把 loader 解析层握在自己手里，之后增加 loader 不再受制于上游索引。

## 做什么

让用户能像今天选 Fabric / Forge 一样，在实例中安装 LabyMod 并完成部署与启动。

## 期望效果

实例配置的 loader 选择中出现 LabyMod；创建、部署、启动、锁定数据等行为与现有 loader 一致。自建 meta 服务是达成它的前置方向，可先行立项。

## 调研所得

- LabyMod 4 不是普通 mod：定位是客户端平台（自带百余项内置功能、自有 addon 生态），处于加载器层，不上架 Modrinth / CurseForge；官方分发只有 Laby Launcher 和面向原版启动器 / MultiMC 格式实例的 installer，对第三方启动器集成不友好。
- 第三方（voxelum minecraft-launcher-core-node）已整理出官方启动器所用的发布 API 全流程：manifest（各 MC 版本对应的 LabyMod 版本与 commit hash）→ 库清单 + 各 MC 版本标准版本 JSON（inheritFrom 原版）→ 主 jar 与原生资源目录 → addon 从其 addon store 按 hash 下载。结构上与现有 loader 同形，启动器侧集成是纯工程问题。
- 该发布 API 没有对第三方的稳定性承诺，随时可能变。这是主要长期维护成本，也是自建 meta 服务应带缓存/固化层的理由之一。
- 普通 Fabric mod 在 LabyMod 下需要其兼容 addon 才能运行，Forge 兼容仅 1.8.9；addon 生态与普通 mod 是两套体系，addon 商店支持可作为二期。
- LabyMod 的版本标识跟随 commitReference 而非常规语义化版本，展示与选择需要适配。
