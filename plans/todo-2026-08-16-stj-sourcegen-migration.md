# STJ 源生成迁移（JsonSerializerContext）

## 背景

发布体积优化（partial trim + Huskui 标注 + grammar 自托管）收官后，`dotnet publish` 剩余 trim 警告 103 条，其中约 90 条属 app 与 Trident 自身代码的 STJ 反射调用（IL2026 家族）。当前 partial 模式下这些调用全部安全（未标注程序集整体保留），**本计划是启用项不是修复项，无紧迫性**——动工前先论证 full trim / NativeAOT 的收益成立（该路径的其余阻塞是 FreeSql、Refit.Reflection、LibGit2Sharp 与 FusionCache 的 MessagePack 序列化器，均不在本计划范围）。

## 做什么

把 Polymerium.Avalonia 与 Trident 两侧所有基于反射的 System.Text.Json 序列化调用迁移到源生成 `JsonSerializerContext`，消灭发布期 IL2026 警告家族，使剩余警告仅限三方包。

## 期望效果

JSON 路径不再依赖运行时反射元数据，成为未来切换 full trim / NativeAOT 时已扫清的前置项。

## 注意事项（调研所得）

- **最大难点是类型驱动的多态分发**：`PersistenceService` 存在按 `Type` 反序列化的调用形态，源生成要求编译期已知 `[JsonSerializable]` 清单——每个走这条路的类型都要注册进 context，漏一个就是运行时 `NotSupportedException`，需要靠测试或启动自检兜底。
- 类型清单分散在两侧仓库（Polymerium 侧的配置/实例/快照/持久化服务，Trident 侧的 profile 与实例管理），迁移要跨 submodule 同规格进行。
- Huskui 侧 GrammarManifest 已示范 STJ source-gen 的 trim 干净用法，可作参照。
