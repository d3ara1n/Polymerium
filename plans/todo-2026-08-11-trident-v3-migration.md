# Trident Profile V3 迁移（过渡计划）

> 状态：构想，未实施。本文描述从 V2 到 V3（B 形态）的演进意图与调研注意事项；实施时对照当下真实代码临场决定 HOW，不回写本文件。

## 现状（V2）

- 实例定义整体存于单个 `profile.json`：元数据、包列表、规则、overrides 全在一个文件里。
- git 视角：任何包变更都重写整个文件，`git status` 只见一个 profile.json 的 ±N 行，无法分辨改了哪些包；`git diff` 同样不可读。
- 包列表顺序承载展示顺序，但无部署语义——部署仲裁按 source tier + SourceOrders 排名，不依赖列表顺序。

## 目标（V3 = B 形态）

- `profile.json` 只剩实例元数据，新增 `format: 3` 标记；`packages/` 目录每包一文件，**目录即唯一真源**，无 manifest。
- git 视角：新增包 = `+ packages/xx.json`，更新包 = `~ packages/xx.json`（文件名不含版本，更新只改文件内容），删除包 = `- packages/xx.json`。
- 额外收益：单包历史（`git log` 限定文件）、单包回滚（`git checkout` 单文件）、merge 冲突收敛到单包文件。
- 规范见 `notes/TRIDENT_V3.md`，以该文档为准。

## 过渡（A 形态）

- 先落 A：`profile.json` 兼作 manifest（有序文件名表）+ `packages/` 每包一文件。
- **为什么先 A 后 B**：A 保留「manifest 即提交点」的原子性语义——批量写包集合时不会出现"写了一半崩溃、丢半批包且不可区分"的状态；加载时以 manifest 为准、清孤儿文件，崩溃安全与 V2 单文件语义等价，迁移风险最小。同时 manifest 显式保序，展示顺序不丢。
- **A → B 的差异只有一个**：去掉 manifest，`packages/` 目录成为唯一真源。
- **演进判定**：A 形态运行稳定、确认目录枚举顺序可接受、且批量写的崩溃安全收益可放弃后，去掉 manifest 走到 B。

## 注意事项（调研结论）

- **文件名 = 项目 slug**：程序创建包文件时必先获取 Package/Project 信息（slug 必得，见 `plans/PACKAGE-SLUG-FIELD.md`），文件名取 `{repository}.{slug}.json`（pref 带 namespace 时全带）；用户手动创建文件时文件名随意，不做约束。文件名不编码版本，更新只改内容。
- **ProfileManager 需要大重构**：V3 的增删改以单包为粒度，现行为整文件读写，改动量大；重构方案实施时对照真实代码确定，此处只记录需要重构，不展开。
- 包列表顺序无部署语义，B 形态以目录枚举（按文件名排序）作为展示顺序即可。
- 批量写（一次装/卸多包）在无提交点的形态下会静默丢半批——这是 B 形态要接受的代价，过渡期由 A 的 manifest 兜底。
- 快照与外部变更监视目前围绕实例主目录 / profile.json，需覆盖 `packages/` 目录。
- Trident 导出包（zip 内 `trident.index.json`）不受影响：zip 不是 git 跟踪对象，无拆分必要；导入侧内存模型不变，天然兼容。
- 兼容迁移有现成先例（Purl → Pref 的过渡属性），本次迁移沿用同思路。

## 不做的

- **格式探测**：用户不关心当前文件是什么格式，只关心最终成品带 `format: 3`；旧文件如何识别、何时转换是实施细节，不在本计划内。
