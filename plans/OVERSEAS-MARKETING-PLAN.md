# Polymerium 海外推广计划（玩家痛点精准线）

> 主线：**玩家痛点精准线** —— 以「秒切加载器 + 原地更新整合包 + 快照兜底」为钩子，主攻 r/feedthebeast + YouTube + Modrinth 社区，稳步建立 MC 玩家基本盘。
> 与 V1/V2 的关系：V1/V2（`archived/MARKETING-PRESENCE-PLAN*.md`）负责**门面建设**（README/官网/文档站/截图/视频脚本），其中大半已落地；本计划负责**海外英语圈的渠道投放、叙事风险处理、社区增长飞轮**，是 V1/V2 未覆盖的增量。

---

## 0. 现状盘点（截至 2026-06-22）

### ✅ 已就绪（V1/V2 落地成果，可直接复用）

| 资产 | 现状 | 海外推广中的作用 |
|---|---|---|
| README（英文为主） | 三代演进叙事、徽章矩阵、平台表、隐私声明 | 开源项目门面，玩家线落地页 |
| 文档站（Fumadocs） | 中英双语、Algolia 搜索、部署在 polymerium.dearain.dev | 承接 SEO 长尾流量 |
| comparison 页（vs-prism-launcher） | **顶级**——有实测数据（5 实例/50 mod/2GB → Prism 10GB vs Polymerium 2GB）、诚实列出 Prism 优势、清晰分人群 | **整条玩家线的转化中枢**，"Prism Launcher alternative" 搜索命中页 |
| 截图素材 | 11 张静态图（instance/packages/accounts/modpacks…） | 凑合可用 |
| PROMO-VIDEO-SCRIPT | 60s 主脚本 + 30s 短版已写好 | 待拍摄 |
| 跨平台三端 | Win x64 / Linux x64 (AppImage) / macOS ARM64 (PKG) 稳定 | 海外分发基础 |
| MIT 开源 | 仓库公开 | 信任背书 |

### ⚠️ 致命短板（玩家线必须补）

| 短板 | 影响 | 优先级 |
|---|---|---|
| **零视频/动图素材** | 玩家圈全靠视觉传播，「秒切加载器 / 原地更新 / 快照回滚」没动图就是干话 | 🔴 P0 |
| **无 Discord** | 海外 modding 社区的核心阵地，没 Discord 等于没社区 | 🔴 P0 |
| **comparison 页 SEO 未强化** | title/description/og:image 未针对 "Prism Launcher alternative" 优化 | 🟡 P1 |
| **Windows Dev Mode 摩擦未主动解释** | 海外 Windows 用户安装时卡在 symlink 权限，**Prism 不需要这步** → 真实流失点 | 🔴 P0 |
| **账号信任信号不够强** | 海外对新启动器第一顾虑是"会不会盗我 MC 账号" | 🟡 P1 |
| **Authlib 叙事风险** | 海外敏感信号，易被贴"破解启动器"标签（Prism 当年因授权争议从 MultiMC 分叉，社区记忆犹新） | 🔴 P0 |

### ❌ 暂不铺（后置）

- 多语言众包（Weblate/Crowdin）——英语够启动，俄/日/西/葡后置
- Product Hunt / HackerNews —— 那是 AI/MCP 破圈线的渠道，玩家线不主推

---

## 1. 差异化牌面与分层叙事

> ⚠️ **2026-06-22 叙事 Pivot**：原计划以「省 60-80% 磁盘」为主钩子，经产品作者澄清后**废弃**——MC 整合包共享同一模组的场景很少，实际节省约 30%，不够震撼，且偏离架构真正的价值。新叙事轴心：**元数据驱动解锁的能力 + 把选择权还给用户 + 快照兜底**。

### 核心洞察（叙事根基）

> 竞品（Prism/MultiMC）创建实例后就把它“冻住”——不能换加载器、不能原地更新整合包版本、不能干净回滚。**不是因为这样更安全，而是它们的文件复制架构一旦铺好就无法重建。**
>
> Polymerium 把实例当作**一份配方**（JSON），改配方、重新部署，几秒就是新实例。**快照**让你敢于折腾——坏了就回滚。
>
> 一句话：**把选择权还给用户，用快照兜底。**

Polymerium 有三张能打海外的牌，受众不同话术分层。玩家线用前两张，第三张留给破圈线：

| 受众 | 主钩子 | 玩家线是否使用 |
|---|---|---|
| **AI / 技术圈**（破圈） | MCP AI Agent 模式（30+ 工具让 AI 直接操控 MC 实例） | ❌ 留给破圈线 |
| **重度 modpack 玩家**（精准） | **元数据驱动解锁的能力**：秒切加载器 / 原地更新整合包 / 快照兜底回滚 | ✅ **玩家线主钩子** |
| **modpack 开发者**（口碑节点） | **实例即 JSON，git 可版本化** + CLI + 快照 Diff | ✅ **玩家线侧钩子**（攻克意见领袖） |

### 核心叙事三件套（玩家线统一口径）

> **主钩子**：`Switch loaders. Update modpacks in place. Roll back anything. — with snapshots as your safety net.`
>
> **一句话定位**：*"A metadata-driven Minecraft instance manager. Your instance is a recipe, not a pile of copied files."*
>
> **必杀场景**（替代旧的磁盘对比）：演示三件**竞品架构上做不到**的事——
> 1. 秒切加载器（Forge → NeoForge，改一行 metadata → 重部署，不重下 mod）
> 2. 原地更新整合包版本（新版 import 覆盖，saves/settings 保留）
> 3. 快照回滚（更新 mod 炸了 → 一键还原 + diff 看变了什么）
>
> 收尾话术：*"Other launchers can't do this — not because it's unsafe, because their architecture can't rebuild an instance. Polymerium can, and snapshots catch you if it breaks."*

### 已废弃的话术（禁用）

- ❌ "省 60-80% 磁盘" 作为主卖点（实际约 30%，震撼不足且偏离价值轴）
- ❌ "三代演进" 自夸叙事（与"把选择权还给用户"的谦逊调性冲突）
- ❌ "Symlink 去重" 作为面向用户的卖点（是实现细节，不是用户价值）
- ❌ "传统的管理文件，我们管理体验"（空洞）

> 注：symlink 去重 / 磁盘节省仍可作为**次要收益**在技术细节中提及，但不作为主钩子。website comparison 页保留存储对比数据（诚实），但叙事重心转向"能力"。

---

## 2. ⚠️ 三个必须先处理的海外叙事风险

玩家线不处理好这三件事，技术再好也会被社区抵制或流失。**全部 P0，引爆前必须完成。**

### 2.1 Authlib 叙事（最敏感）

Polymerium 带 Authlib Injector（第三方 Yggdrasil 伪服登录），这是中国社区刚需，但在欧美圈是**敏感信号**——海外玩家对"绕过正版验证"高度警惕。

**处理方式：**
1. 英文官网/README/comparison 把 Authlib 描述为**"兼容性功能（为私有服务器生态）"**，**不作为卖点**；
2. **把 Microsoft OAuth (Device Code Flow) 摆在最显眼位置**，明确"正版账号是一等公民"；
3. 海外宣传素材（Reddit 帖子、YouTube 描述、OG image）**完全不提** Authlib（留给中文社区）；
4. comparison 页 Account Management 段保持现状（诚实列出），但英文措辞改为 `Third-party auth servers (Yggdrasil-compatible)`，避免"破解/绕过"联想。

### 2.2 Windows Developer Mode 摩擦（最真实的安装流失点）

Polymerium 要 symlink → 海外 Windows 用户必须开 Developer Mode，**Prism 不需要这步**。这是硬伤，不能藏。

**处理方式：**
1. README 安装区**主动、积极地讲清楚**——已有的 `<details>` 折叠块要**升级为醒目 callout**（不再折叠）；
2. 配一条 **15 秒 GIF**：开 Developer Mode 的三步操作 + 一句"为什么要开 / 开了对你电脑无害"；
3. 所有投放素材里**主动提及**，建立"一次性 30 秒设置解锁元数据驱动能力"的权衡话术。

### 2.3 账号信任信号（新启动器的生死线）

海外玩家对新启动器第一顾虑是"会不会盗我 MC 账号"。README 已有 Privacy & Security 段，但位置太靠后。

**处理方式：**
1. **下载按钮旁**加一句强信号：`Microsoft OAuth (Device Code Flow) · Open source MIT · Account passwords never stored`；
2. MIT + 开源是最大信任背书，顶到 Hero 区；
3. 首次启动 OOBE 已有隐私确认，确保英文文案强调"credentials never leave your machine"。

---

## 3. 三周作战图

### Week 1 · 弹药（物料准备）

| # | 物料 | 谁做 | 说明 |
|---|---|---|---|
| 1.1 | **能力演示 GIF**（核心广告，替代旧的磁盘对比） | 🟦 需拍 | 三件事并排：秒切加载器（Forge→NeoForge）/ 原地更新整合包 / 快照回滚炸了的实例 |
| 1.2 | **"竞品做不到" 对比 GIF** | 🟦 需拍 | 左右分屏：Prism 换加载器需重建实例 vs Polymerium 改一行秒切 |
| 1.3 | **Windows Dev Mode 设置 GIF**（30 秒） | 🟦 需拍 | 配合叙事风险 2.2 |
| 1.4 | **OG image / social card**（1200×630） | 🟩 可做 | 官网 + Reddit 分享时自动带的预览图，复用品牌色 |
| 1.5 | comparison 页顶部嵌「30 秒看懂」GIF | 🟩 可做 | 用 1.1 的 GIF |

### Week 2 · 阵地（承接流量）

| # | 动作 | 谁做 | 说明 |
|---|---|---|---|
| 2.1 | **建 Discord 服务器** | 🟦 用户 | 频道：`#welcome` `#support` `#showcase` `#modpacks` `#cli-mcp`；置顶 comparison 链接 |
| 2.2 | README 加 **Discord 徽章 + 账号安全强信号** | 🟩 可做 | 下载按钮旁（叙事风险 2.3） |
| 2.3 | README Dev Mode callout 升级（折叠→醒目） | 🟩 可做 | 叙事风险 2.2 |
| 2.4 | comparison 页 **SEO 强化** | 🟩 可做 | title/description 调成 `Polymerium vs Prism Launcher — Switch Loaders · Snapshots · In-Place Updates`；加 og:image；确保 Google 索引 |
| 2.5 | 注册 Reddit/YouTube 账号，**潜伏 1 周** | 🟦 用户 | 在 r/feedthebeast 看文化、回答问题攒 karma，**别急着发广告** |

### Week 3 · 投放（引爆）

| # | 动作 | 谁做 | 说明 |
|---|---|---|---|
| 3.1 | **r/feedthebeast 发帖** | 🟩 可写草稿 | 标题候选见 §4.1；以个人痛点故事开场，附能力演示 GIF，结尾轻提 Polymerium。**禁止广告腔** |
| 3.2 | **YouTube 外联**（10 个订阅 5k–50k 的 modpack 评测作者） | 🟩 可写私信模板 | 给独家选题"秒切加载器 + 原地更新整合包的实测"，提供 ready-to-use modpack 包 |
| 3.3 | **Modrinth / CurseForge 社区**以"分享经验"姿态出现 | 🟦 用户 | 在相关 modpack 讨论区回答问题，自然带出 |
| 3.4 | 所有投放链接指向 comparison 页 | — | 让 SEO 反向吃 Reddit/YouTube 的长尾流量 |

---

## 4. 渠道打法（按杠杆排序）

### 4.1 第二梯队：精准玩家圈（玩家线主战场）

**Reddit**（最核心）：
- **目标子版块**：`r/feedthebeast`（核心，modpack 玩家聚集）、`r/Minecraft`、`r/ModdingMinecraft`
- **规则严，先提供价值再提产品**——回答问题、分享 modpack 管理经验，否则会被 ban
- **标题候选（个人痛点故事开场，禁止广告腔，聚焦“能力”而非“省磁盘”）：**
  - `I wanted to switch from Forge to NeoForge without rebuilding my whole instance. So I built a launcher that can.`
  - `Tired of re-importing modpacks every update? Here's how I do in-place updates now.`
  - `Other launchers freeze your instance the moment you create it. Here's why their architecture can't let you experiment — and what happens when it can.`
- **正文结构**：痛点（具体数字，引用 Prism 真实 issue 原话）→ 发现过程 → 对比截图 → 结尾一句"by the way, I open-sourced this" → 链接 comparison 页

**YouTube**：
- 找中小型 modpack 评测作者（订阅 5k–50k），**不找头部**（头部不理人）
- **私信要点**：① 提供独家选题（"秒切加载器 + 原地更新整合包的实测"）② 提供 ready-to-use modpack 包（省对方准备成本）③ 强调 MIT 开源、可商用 ④ 附能力演示 GIF 让对方一眼看懂卖点
- MC 内容在 YouTube 是流量黑洞，一个 5 万订阅作者的实测视频顶 100 条 Reddit 帖

**Modrinth / CurseForge**：
- 在 modpack 讨论区、官方 Discord 以"分享经验"姿态出现
- 不发广告，回答"怎么管理多个 modpack"类问题，自然带出

### 4.2 第三梯队：长期资产（持续运营）

**SEO**（文档站已有底子）：
- 目标关键词：`Prism Launcher alternative`、`save disk space Minecraft mods`、`Minecraft launcher symlink`、`Minecraft instance manager`
- comparison 页是最大 SEO + 转化入口，搜 "Prism Launcher alternative" 的人直接命中
- 每篇文档配 og:image，加 JSON-LD SoftwareApplication 结构化数据

**社区翻译**（后置）：
- 上 Weblate/Crowdin 开放众包
- 优先级：俄语、日语、西语、葡语（俄罗斯和巴西是 MC 大市场）
- 玩家线英语启动后即可铺开

**被收录**：
- 争取进 "Best Minecraft Launchers 2026" 类榜单（主动联系博主）
- PR 进 Awesome lists（Avalonia、Minecraft ecosystem）

### 4.3 社区主导增长飞轮（CLG，零成本核心引擎）

调研结论：开源项目的增长飞轮是 **开源 → 社区信任 → 口碑 → 贡献者 → 更好产品 → 更多用户**。早期 **90% 时间花在 Reddit/Discord "帮人解决问题"**，而不是发广告。

**玩家线的真正飞轮**：
> 一个 modpack 作者用 Polymerium 秒切加载器 + 快照调试 + Git 管理发布 → 他在 modpack README 里写"推荐用 Polymerium 打开" → 玩家跟着来。

所以**早期重点攻克 modpack 作者**（而非散客玩家）——给他们一份「用 Polymerium + Git 开发 modpack」的英文爆款指南（V1/V2 已有 `guides/git-collaboration`、`guides/modpack-dev` 文档，翻译润色成英文爆款即可）。modpack 作者一句推荐顶 100 条广告。

---

## 5. 与 V1/V2 的衔接关系

| V1/V2 计划项 | 当前状态 | 本计划如何衔接 |
|---|---|---|
| V1 Phase 0 README 重写 | ✅ 已完成（英文为主） | 本计划仅微调：加 Discord 徽章、账号安全强信号、Dev Mode callout 升级、Authlib 措辞调整 |
| V1 Phase 1 Landing Page | ✅ 已用 Fumadocs 建成（polymerium.dearain.dev） | 本计划不再建站，聚焦 SEO 强化 + comparison 页打磨 |
| V1 Phase 2 文档站 | ✅ 已建成（getting-started/concepts/managing/advanced/guides/comparisons） | 本计划复用 `guides/git-collaboration`、`guides/modpack-dev` 作为 modpack 作者攻克物料 |
| V1 Phase 3 截图与视觉资产 | ⚠️ 11 张静态图已有，零动图 | 本计划 Week 1 补 3 条 GIF（能力演示/秒切快照/Dev Mode） |
| V1 Phase 4 社区（Discord+QQ） | ❌ Discord 未建 | 本计划 Week 2 建 Discord（QQ 群是中国社区，玩家线海外不铺） |
| V1 Phase 5 CLI & MCP 宣传 | ⚠️ 文档已有，宣传未启动 | **留给 AI/MCP 破圈线**，玩家线不主推 |
| V1 Phase 6 视频演示 | ⚠️ 脚本已写（PROMO-VIDEO-SCRIPT.md），未制作 | 本计划 Week 1 的 3 条 GIF 是其短视频版，主推视频后续制作 |
| V1 Phase 7 中国市场专项 | — | 与本计划互补，中国社区是 Polymerium 基本盘 |
| V1 Phase 8 SEO 与增长 | ⚠️ 基础未铺 | 本计划 Week 2 强化 comparison 页 SEO |
| V2 特色全景 / 文档内容规划 | ✅ 已落地 | 本计划直接复用 comparison 页（V2 竞品矩阵的实化） |

---

## 6. 可立刻落地的物料（分两类）

### 🟩 我（助手）能直接做的（文档/代码层面）

| # | 物料 | 类型 |
|---|---|---|
| A1 | README 微调：Discord 徽章、账号安全强信号（下载按钮旁）、Dev Mode callout 升级、Authlib 英文措辞调整 | 文档编辑 |
| A2 | comparison 页 SEO 强化：title/description/og:image 占位、锚点、"30 秒看懂" GIF 嵌位 | 改 mdx + meta |
| A3 | Reddit 帖子英文文案包（3 篇候选标题 + 完整正文草稿） | 内容创作 |
| A4 | YouTube 外联私信英文模板（含独家选题话术） | 内容创作 |
| A5 | `guides/git-collaboration` / `guides/modpack-dev` 英文指南润色成"爆款"（modpack 作者攻克物料） | 文档润色 |
| A6 | OG image / social card 设计稿（SVG/Figma 指令） | 设计指令 |
| A7 | Weblate 社区翻译集成准备（i18n 接入方案） | i18n 接入 |
| A8 | comparison 页加 JSON-LD SoftwareApplication 结构化数据 | SEO |

### 🟦 需要用户做的（需真实运行环境/账号/人际关系）

| # | 物料 | 原因 |
|---|---|---|
| B1 | 3 条核心 GIF/短视频（能力演示/秒切快照/Dev Mode） | 需真实运行环境录屏 |
| B2 | Discord 服务器创建 + 频道配置 | 需账号所有权 |
| B3 | Reddit/YouTube 账号注册 + 潜伏攒 karma | 需账号 + 时间 |
| B4 | YouTube 作者外联发送 | 需人际关系 + 持续跟进 |
| B5 | r/feedthebeast 发帖（账号 karma 够之后） | 需账号声誉 |
| B6 | KLPPBS / B 站 / 知乎（中国社区，玩家线海外不主推，但可同步） | 需中文社区账号 |

---

## 7. 指标与目标

| 指标 | 当前（估） | 1 个月目标 | 3 个月目标 |
|---|---|---|---|
| GitHub Stars | ~100 | 150 | 300 |
| Discord 成员 | 0 | 30 | 150 |
| 官网（polymerium.dearain.dev）月访问 | 低 | 500 | 3,000 |
| comparison 页月浏览 | 低 | 200 | 1,500 |
| 下载量/月 | 未知 | 追踪 | 追踪 |
| YouTube 合作视频数 | 0 | 1 | 3 |

**分析工具：**
- GitHub Insights（Stars/Forks/Traffic）
- Vercel Analytics 或 Umami（官网访问量，隐私友好）
- Discord Insights（社区活跃度）
- Algolia 搜索词分析（文档站内搜索热度）

---

## 8. 风险与应对

| 风险 | 概率 | 应对 |
|---|---|---|
| 被贴"破解/盗版启动器"标签（Authlib） | 高 | 叙事风险 2.1 全套处理；主动强调 MIT + Microsoft OAuth 一等公民 |
| Windows Dev Mode 导致安装流失 | 高 | 叙事风险 2.2 全套处理；15 秒 GIF + 权衡话术 |
| Reddit 账号被 ban（自推广检测） | 中 | 潜伏期先攒 karma；帖子以个人痛点故事开场，禁止广告腔；遵守各子版块规则 |
| YouTube 作者不回复 | 中 | 列 10 个，预期回复率 20%；提供 ready-to-use modpack 包降低对方成本 |
| comparison 页 SEO 起效慢 | 中 | Google 索引需 4-8 周，Week 2 就铺，不等待 |
| Prism 社区敌意 | 低 | comparison 页诚实列出 Prism 优势，不拉踩；强调"不同用例选不同工具" |

---

## 附录 A：玩家线叙事资产清单

### 核心数据点（反复用）

- **秒切加载器**（改一行 metadata → 重部署，不重下 mod）
- **原地更新整合包**（新版 import 覆盖，saves/settings/persist 保留）
- **快照回滚**（内容寻址，一键还原 + diff）
- **磁盘节省约 30%**（次要收益，不作主钩子；symlink 是实现细节）
- **30+ CLI 命令 + MCP 模式**（玩家线侧钩子，不主推）
- **MIT 开源**（信任背书）

### 引用素材（真实用户原话）

来自 Prism GitHub Issues 的真实痛点，可作为 Reddit 帖子开场（V1 附录 B 已收录）：

> "I recently saw I almost use 25GB of data for Prism Launcher." — Prism #1052
>
> "Utilize Central Mods Folder and Symlinks to Cut Down on Mods Across Multiple Modpacks Eating Up Storage" — Prism #2611
>
> "Same mod files are installed over and over again." — CurseForge Ideas CF-I-7706

### 禁用话术（海外敏感）

- ❌ "破解 / 盗版 / 绕过验证"
- ❌ 把 Authlib 作为卖点宣传
- ❌ "比 Prism 更好"（拉踩）→ ✅ "不同用例选不同工具"
- ❌ "next-generation / thinks differently"（空洞自夸，V1 已指出）

---

## 附录 B：玩家线后续可演进路径

玩家线建立基本盘后，可平滑切换到**AI/MCP 破圈线**（本计划未展开，备查）：

- 主钩子切换为 MCP AI Agent 模式
- 渠道切换为 HackerNews Show HN + r/aiAgents + r/MCP + 技术博客
- 叙事从"切换加载器 / 快照兜底"升级为"让 AI 管理你的游戏库"
- 引爆点：一条 "I built an MCP server into a Minecraft launcher" 的技术文

玩家线的社区基本盘（Discord/Reddit karma/YouTube 关系）会成为破圈线的承接池。
