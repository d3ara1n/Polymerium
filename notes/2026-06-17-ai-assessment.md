# AI 使用情况与 Vibe Coding 评估报告

| 项目 | 值 |
|---|---|
| 报告日期 | **2026-06-17** |
| 分析对象 | Polymerium（`d3ara1n/Polymerium`，仓库根 `Polymerium.slnx`） |
| 分析者 | pi coding agent（基于 git 历史 + 源码静态取证） |
| 分析方法 | git 提交取证、代码注释/密度统计、文档入库时间线、人工特征识别 |
| 结论 | **非 vibe coding。人类主导、AI 辅助（仅限计划与评审）的 augmented 项目。** |

---

## 一、结论（TL;DR）

- **不是 vibe coding 产物。** 每一份进入 `main` 的代码背后都有人类的判断、签名、踩坑笔记与评审闭环。
- **代码本体始终由人类手写。** 维护者明确：所用 AI 的代码质量不足以直接采纳，故仅用于**计划撰写**与**代码评审**两类辅助。
- **AI 辅助于 2026 年早些时候引入**，其可见产物是 `plans/` 下的设计文档与评审报告。代码库的骨架与实现早于 AI 引入。
- 真正贴切的标签是 **"AI-augmented craftsmanship"（AI 增强的匠人式开发）**——AI 加速了规划与评审环节，但未染指实现主导权。

---

## 二、方法论纠偏（重要）

本报告的第一稿曾错误地从「2024 年末提交数翻倍」推断 AI 介入。**经查证，该推断不成立**——提交节奏的突变另有成因（见下）。记录如下作为教训：

> ❌ 错误信号：**提交频率 / 月度提交曲线**。
> 它是高度混淆的指标，无法单独用于判定 AI 介入。

提交节奏突变的真实成因（均已查证，非推断）：

1. **技术栈迁移的空窗（git 可查）**：仓库同时保留三个技术栈分支 `origin/winui`、`origin/tauri`、`origin/avalonia`。`origin/tauri` 的最后提交停在 **2024-10-11**，且其提交信息写满了失败挣扎的痕迹（`tauri 2.0 breaks everything` / `实在无法解决居中问题，pandacss资料太少` / `webkit2gtk breaks` / `tauri leaves a blank screen on Linux wayland`），有 164 个 main 没有的提交。对应 2023-08 ~ 2024-09 主线的低活动期，即 Tauri 实验搁置期。
2. **2024 年末的暴涨**：Avalonia 重写不仅是架构革新，还要大量补齐 Avalonia 缺失的基础设施代码，故提交量陡增。**与 AI 无关。**
3. **Jira 驱动的提交粒度变化（Jira MCP 可查）**：Jira 项目 `POLY` 建于 **2025-11-08**（POLY-1 创建于 2025-11-08 14:28、POLY-2 创建于 14:29，同日相邻分钟）。引入 Jira 后从「一天结束统一提交」改为「按文件/目的拆分提交」，进一步推高提交数量。

**结论：提交曲线只能反映工程节奏，不能作为 AI 信号。** 判定 AI 介入应改用更硬的代理指标——见第四节。

> **方法论副产品（也是非 vibe coding 的旁证）**：Jira 于 2025-11-08 建立，但 commit 里首次出现 `POLY-` key 是 **2025-11-28**（POLY-5），间隔约 3 周。这 **不是**“工具采用的滞后”——Jira 一建好立即可用，不存在采用成本。这 20 天是**真实的设计与实现时间**：issue 创建后需要先构想架构、编写代码，才能产生可提交的成果。以 POLY-1「通知系统大改造」为例，创建于 2025-11-08，更新记录一直延续到 2026-04-09——**单个 issue 跨近 5 个月持续打磨**。这恰恰是人类节奏的铁证：靠 AI 的 vibe coder 会在 issue 建立的几小时内就产出第一条 commit。也就是说，“issue 创建与首条 commit 的间隔”测量的是**工程耗时**，不是 AI 信号。

---

## 三、项目真实历史（均已查证）

| 阶段 | 时间 | 查证来源 | 性质 |
|---|---|---|---|
| WinUI 起步 | 2023-01-12 起 | git `origin/winui` | 人类手写（早于 AI 编程工具成熟期） |
| Tauri 分支实验 | 2023-08 ~ 2024-10 | git `origin/tauri`（末次提交 2024-10-11，164 个独有提交，提交信息含 `tauri 2.0 breaks everything` 等失败痕迹） | 失败、搁置 |
| Avalonia 主线重写 | 2024-10 起 | git `origin/avalonia` | 人类手写；含大量基础设施补齐代码 |
| Jira 任务管理引入 | **2025-11-08** | Jira MCP：POLY-1 创建 2025-11-08 14:28、POLY-2 创建 14:29 | 工作流变化，提交粒度细化 |
| AI 辅助（计划 + 评审） | 2026 年早些时候起 | git `plans/` 文档入库日期（见第四节） | **代码仍手写**，AI 仅用于文档与评审 |

---

## 四、AI 辅助的真实时间线（硬证据：文档入库日期）

以 `git log --diff-filter=A`（文件首次入库日期）为准：

| 文件 | 首次入库 | 说明 |
|---|---|---|
| `AGENTS.md` | 2026-04-26 | 给 AI 助手的项目规范（早期雏形） |
| `plans/GC-DESIGN.md` | 2026-05-31 | 最早的设计文档 |
| `plans/`（目录建立） | 2026-05-19 起 | 计划文档开始集中出现 |
| `ROLLING.md` | 2026-06-13 | changelog 文案规则（为 AI 助手制定） |
| `plans/CODE-REVIEW-2025-06-11.md` | 2026-06-11 | 代码评审报告（文件名中的 2025 为编号/会话标签，实际入库 2026） |
| `plans/DIFFVIEW-HUSKUI-CODE-MIGRATION.md` | 2026-06-14 | 迁移计划 |
| `plans/MAINWINDOW-DECOUPLING.md` | 2026-06-15 | 解耦计划 |
| `plans/INSTANCE-STATE-AGGREGATOR.md` | 2026-06-16 | 状态聚合层设计（分 Phase 1/2/3） |
| `plans/REVIEW-REPORT.md` | 2026-06-17 | 结构化代码审查报告 |

**解读**：所有 AI 可见的结构化产物（计划 + 评审）都密集出现在 **2026-05 ~ 06**，与维护者所述"今年早些"完全吻合。在此之前（含整个 Avalonia 重写期）没有此类文档——证明那段时期的代码产出是纯人工的。

---

## 五、判定为「非 vibe coding」的证据

### 5.1 工程纪律指标（与 vibe coding 相反）

| 指标 | 本项目 | vibe coding 典型值 | 解读 |
|---|---|---|---|
| `TODO/FIXME/HACK` 数量 | 全 `src` 仅 11 处，其中约半数是 `Program.MagicWords` 误命中，真实债务注释 ~5 条 | 几十至上百 | 克制 |
| 注释 / 代码比 | ≈ 4.9%（1454 行注释 / 29946 行非空代码） | 常 10~20%+，多废话复述 | 只在"为什么"处下笔 |
| 中英文注释比 | 489 中文 : 498 英文 ≈ 1:1 | 单一语言为主 | 中文母语者真实习惯 |
| 过度文档化文件 | 几乎无（仅 `ProxySettingsModel.cs` 46%，属正常属性文档） | 大量畸高文件 | 无 AI 啰嗦症 |
| 提交规范 | 全程 Conventional Commits + scope，关联 Jira/Sentry/GitHub | `wip`/`final v2`/`fix typo again` | 严明 |

### 5.2 代码内的"人类签名"（vibe coding 写不出）

- **`Program.cs`**：
  - `public static readonly string MagicWords = "say u say me";` —— Lionel Richie 歌词梗
  - `public static readonly string MirrorChyanCdk = "0001bf...";` —— 把镜像服务 CDK 硬编码进源码（AI 一定会劝你放进配置）
  - `#region 8.2 杂七杂八的服务处置` —— "杂七杂八"是口语俚语
  - `// PROCEDURE MOVED: ...` —— 跨文件记忆迁移备忘
  - `GetSafeCultureInfo` 同时 catch `CultureNotFoundException` 与 `ArgumentException` —— 踩坑式防御

- **`Services/InstanceStateAggregator.cs`**（System.Reactive + DynamicData 高级响应式）：
  - 注释解释的是**架构决策与 Rx 内存语义**，非复述代码
  - `实体服务不抽接口：消费方直接依赖具体类。生命周期 = 应用` —— 带理由的资深取舍
  - `// 进度节流更新（沿用现状 1s 策略）` —— "沿用现状"表明是重构中刻意保留的旧决策，只有重构者本人才会这么写
  - 关于 `Defer`/`RefCount`/`Replay(1)` 的语义笔记 —— 调试过响应式泄漏后留下的记录

- **`CODE-REVIEW-2025-06-11.md`** 中的处理记录是决定性证据：
  > ✅ #8（ProfileManager 的 sync-over-async）— **彻底解决，方式与 Review 原建议不同。** Review 只看到 `Add` 的 `.Wait()`，但核实后整个 `SaveAsync` 链都是「写几 KB 本地 JSON、async 获益≈0」的伪异步……顺带修掉 `Update` 的同款 `.Wait()`（**Review 未列**）。

  **人类发现了 AI 评审的盲点，并给出比 AI 建议更深的解法。** vibe coder 不会质疑评审，只会照单全收。这是"人类主导"的标志。

### 5.3 AI 辅助工作流文档（人类驾驭 AI，而非被 AI 驱动）

- `REVIEW-REPORT.md`：结构化变更清单（新增/删除/修改文件表）—— AI 生成的评审报告典型形态，被人类归档并跟踪。
- `INSTANCE-STATE-AGGREGATOR.md`：重构前的分阶段设计文档（背景与动机 / 设计原则务必遵守 / record 三层归一化模型），提交中的 `Phase 2`/`Phase 3` 对应此计划。**有计划、有阶段、有评审闭环 = vibe coding 的反面。**
- `AGENTS.md` + `ROLLING.md`：维护者为 AI 助手手写的苛刻文风规则（动词起首、禁冒号、issue key 收尾括号等）——**AI 是被驯服的工具**。

---

## 六、判定为「AI 辅助（augmented）」的证据

- `plans/` 全部设计/迁移/评审文档集中出现在 2026-05 ~ 06，形态高度结构化，符合 AI 生成文档特征。
- `CODE-REVIEW` / `REVIEW-REPORT` 采用编号 issue 逐条跟踪的格式，是 AI 评审助手的标准输出。
- 代码本体仍为手写：AI 仅介入计划与评审环节（2026 年早些时候起），此与代码内大量「踩坑式」人类签名、Rx 语义笔记互为印证。

---

## 七、最终判定

| 维度 | 判定 |
|---|---|
| 纯人类手写 | 2023-2026 代码骨架与实现是 |
| 纯 AI / vibe coding | **否** |
| AI 辅助（augmented） | **是**：2026 年早些时候起，**仅限计划撰写 + 代码评审** |
| 代码理解度 | 高（Rx、DI、Avalonia 生命周期均有深入掌控） |
| 人类主导权 | 强（评审闭环 + 设计文档 + 多处纠正 AI 的痕迹） |

**一句话**：Polymerium 是资深开发者（Chien Zhang / @d3ara1n）主导的项目，AI 在其中扮演"计划草拟 + 代码评审"的加速器角色，未染指实现主导权。**绝非 vibe coding。**

---

## 八、附录：关键数据快照（2026-06-17 采集）

- 仓库历史：2023-01-12 ~ 2026-06-17（约 3.5 年），主要作者 Chien Zhang / zqy02。
- **技术栈分支（`git branch -a`）**：`origin/winui` / `origin/tauri`（末次 2024-10-11，164 独有提交）/ `origin/avalonia` / `origin/main`——三条迁移路径完整保留。
- **Jira 项目（MCP 实查）**：`POLY`（Polymerium），POLY-1 创建于 2025-11-08 14:28、POLY-2 创建于 2025-11-08 14:29。
- 代码规模：`src` 下 328 个 `.cs` 文件 / 29946 行非空代码；`submodules/Trident.Net` 302 个 `.cs` 文件。
- 最大文件：`Resources.Designer.cs`（6198 行，生成产物）、`InstanceSetupPageModel.cs`（1346 行）。
- Git 总变更：19122 文件次、+372382 / -255846 行。
- 月度提交峰值：2026-04（173 次）、2025-12（115 次）—— 峰值与 AI 无关，源于 Avalonia 基础设施补齐与 Jira 细粒度提交。

---

## 九、方法论备忘（供未来复核参考）

判定 AI / vibe coding 时，信号可信度从高到低：

1. ✅ **强信号**：文档入库日期（`--diff-filter=A`）、代码内踩坑笔记与俚语、人类纠正 AI 评审的记录、注释解释"为什么"而非"是什么"。
2. ⚠️ **中信号**：注释密度与中英混用习惯、提交信息规范度、TODO/FIXME 绝对数量。
3. ❌ **弱/误导信号**：**提交频率曲线**（被技术栈迁移、工作流变化严重混淆）、代码"看起来整洁"（AI 也能写出整洁代码）、文件数量增长。
4. ⚠️ **容易被误读的信号**：**issue 创建与首条 commit 的间隔**。首版报告曾把它当“工具采用滞后指标”，实为错误归因——该间隔测量的是**真实工程耗时**（设计 + 实现），而非 AI 介入证据。反过来说，间隔较长（本仓 POLY-1 跨近 5 个月、issue 创建到首条 `POLY-` commit 约 3 周）本身是**人类节奏的旁证**，vibe coding 场景下该间隔通常以小时计。

> 本报告首版误用了第 3 类信号（提交频率），并对 issue→commit 间隔做出错误归因；现均已以 git 分支与 Jira MCP 实查证据纠正。未来对本仓的复核应优先依赖第 1 类信号。
