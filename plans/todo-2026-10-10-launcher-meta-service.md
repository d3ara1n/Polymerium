# 自建 Launcher Meta 服务

追踪：POLY-181（Epic）/ POLY-183（任务）；关联计划 todo-2026-10-10-labymod-loader-support.md（POLY-182）、Flint 接线（POLY-184），均依赖本项落地。

## 背景

Trident 的 loader 元数据（版本清单与版本 JSON）完全经由 PrismLauncher 的公共 meta 服务解析。上游索引不由我们控制，分发渠道不在其中的 loader 就接不进来——预留多年的 Flint 身份因此未接线，想支持 LabyMod 时再次撞上这堵墙，遂决定把元数据层拿回自己手里。代码里 PrismLauncherService 上遗留的「日后换成 PlatformService」TODO 是同一方向的伏笔。

## 做什么

自建 launcher meta 服务接管 loader 元数据解析：对外提供与 PrismLauncher component 格式兼容的索引与版本 JSON；起步形态为聚合镜像 Prism meta 并注入自有条目；对无稳定性承诺的上游提供快照固化层；Trident 侧完成消费端切换。

## 期望效果

- 新增 loader 只需往自家索引加条目，不再依赖上游放行
- Trident / Polymerium / CLI 消费格式不变，仅替换端点，现有 PrismLauncher 由通用服务取代
- 上游（Prism meta、LabyMod 发布 API 等）变动或不可用时，自有服务照常提供服务

## 调研所得

- PrismLauncher 的 component index / component JSON 格式本身成熟，保持格式兼容可把「聚合还是自建」的边界留在服务端，客户端零感知，日后演进不动客户端
- LabyMod 发布 API 没有对第三方的稳定性承诺，快照固化层是必要缓冲（详见 LabyMod 构想）
- LabyMod 的版本标识跟随 commitReference 而非语义化版本，索引与展示设计不要假设 semver
