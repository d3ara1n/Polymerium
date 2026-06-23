# Polymerium 海外推广计划 V3

> 写作日期：2026-06-23
> 作者：Alma
> 替代 V2（OVERSEAS-MARKETING-PLAN.md），取其精华去其糟粕，不做新旧对比。

---

## 0. 核心判断

### 0.1 V2 最大的盲点：把"海外"当成一个市场

V2 的计划书把海外等同于英语高收入正版圈（r/feedthebeast、YouTube 英文区、Modrinth），然后小心翼翼藏 Authlib、强调正版一等公民。

**这个策略对那个圈子是对的，但它漏掉了另一半地球。**

Prism Launcher 被疯狂 fork（PollyMC、FjordLauncher、PrismLauncher-Cracked、FreesmLauncher…）本身就证明了一件事：**有一大批玩家，他们用英文界面、上 GitHub、在同一个生态里活跃，但他们需要离线模式、第三方验证服务器、无 DRM 的启动器。**

这些人分布在俄罗斯、巴西、东南亚、印度——MC 最大的几个非英语市场。

### 0.2 PollyMC 教我们的事

PollyMC 比 Prism 不"更强"。它的唯一卖点就是三件事：

1. **去 DRM**：不需要先登录正版账号就能用离线模式
2. **加 Ely.by**：第三方验证服务器支持
3. **恢复 CurseForge/FTB 下载**：Prism 上游移除了，他们加回来

就这三样。没有架构创新，没有元数据驱动，没有快照回滚。但它活得很好——因为 **Prism 的 DRM 制造了需求**。

Polymerium 不需要学 PollyMC 的功能，因为 Polymerium 自己就没加 DRM。你需要学的是 **PollyMC 的用户定位**。

### 0.3 这张计划书干什么

**不再把 Authlib 当敏感词藏起来，而是按市场分层。**

| 市场 | 阵地 | Authlib 策略 | 主钩子 |
|---|---|---|---|
| 英语正版圈 | Reddit/YouTube/Discord | 低调，不主动提 | 秒切加载器 + 快照回滚 + 原地更新 |
| 非英语平行市场 | VK/Telegram/巴西论坛/印尼 Discord | 大方写，"私有服务器生态兼容" | 功能 + 自由度 + 不设卡 |
| AI/技术圈 | HackerNews/Blog | 无关 | MCP AI Agent 模式 |

---

## 1. 三张牌

### 1.1 玩家主牌：元数据驱动

> **一句话**：其他启动器创建实例后就"冻住"了——不能换加载器、不能原地更新整合包、不敢折腾。Polymerium 把实例当作一份配方（JSON），改配方、重部署，几秒就是新实例。快照兜底。

**三件竞品架构做不到的事：**

1. **秒切加载器**：Forge → NeoForge，改一行 metadata → 重部署，不重下 mod
2. **原地更新整合包**：新版 import 覆盖，saves/settings 保留
3. **快照回滚**：更新 mod 炸了 → 一键还原 + diff 看变了什么

### 1.2 玩家侧牌：modpack 开发者工具

> 实例即 JSON，git 可版本化。用 Git 管理 modpack 开发，CLI + 快照 Diff 调试，一键发布。

这是攻克 modpack 作者的物料。一个 modpack 作者一句"推荐用 Polymerium 打开"顶 100 条广告。

### 1.3 破圈牌（玩家线不主推）

> MCP AI Agent 模式：30+ 工具让 AI 直接操控 MC 实例。

玩家线建立基本盘后再切换。现在不提。

---

## 2. 关键的诚实：那些 V2 不敢说但必须说的话

### 2.1 关于 Authlib

**事实**：Polymerium 带 Authlib Injector（第三方 Yggdrasil 伪服登录）。这是中国社区刚需，在俄/巴/东南亚同样是刚需。

**V3 策略**：分层处理。

- **英语正版圈**（Reddit/Discord 英文频道）：
  - 不主动提 Authlib
  - 如果被问到，回答："Polymerium supports Yggdrasil-compatible auth servers for private server ecosystems. Microsoft OAuth is first-class."
  - 关键词是 "private server ecosystems"，不是 "offline" 或 "cracked"

- **非英语平行市场**（VK、Telegram 俄语群、巴西论坛）：
  - 大方写进功能介绍
  - 措辞："兼容 Ely.by 等第三方验证服务，适合私有服务器、家庭服务器、局域网联机"
  - 这不是盗版文案，这是实际使用场景

- **官方文档/网站**：
  - Account Management 部分列出所有支持的账号类型，Microsoft OAuth 排第一
  - Authlib 描述为 "Third-party auth servers (Yggdrasil-compatible)"，不做道德判断
  - **不要学 Prism 给离线账号加锁**——Polymerium 本身没这层 DRM，这是优势

### 2.2 关于 Windows Developer Mode

这是真实的流失点。Prism 不需要开 Developer Mode，Polymerium 要（因为 symlink 需要）。

**不藏，不道歉，直接讲清楚**：

- 安装指南用醒目的 callout 说明：为什么要开、三步操作、30 秒搞定
- 配 15 秒 GIF
- 话术："一次 30 秒设置，解锁元数据驱动能力。Windows Developer Mode 是微软官方功能，不会影响系统安全。"
- 比较页诚实列出：Prism 不需要这一步，Polymerium 需要。让用户自己选。

### 2.3 关于账号安全

海外新启动器的生死线：玩家第一反应是"你会不会盗我 MC 账号？"

- 下载按钮旁加一行：`Microsoft OAuth · Open source MIT · Credentials never leave your machine`
- MIT 开源顶到 Hero 区——代码可审计
- OOBE 隐私确认英文文案强调 "your credentials are stored locally only"

### 2.4 关于不要藏的东西

V2 花了很多篇幅讨论怎么"藏"、"不要提"、"低调处理"某些特性。

V3 的立场：**除了在英语正版社区不提 Authlib 作为卖点，其他所有能力大胆讲。** 因为你不是什么灰色软件——你是一个 MIT 开源的、功能强大的启动器。Authentic confidence beats hiding.

---

## 3. 周级作战

### Week 1 — 弹药（先有东西给人看）

| # | 物料 | 说明 |
|---|---|---|
| 1 | **核心演示视频/GIF**：秒切加载器 | Forge → NeoForge 改一行 metadata → 重部署。15 秒。这是最强的钩子。 |
| 2 | **核心演示 GIF**：快照回滚 | 更新 mod 炸了 → diff 看变了什么 → 一键回滚。20 秒。 |
| 3 | **Windows Dev Mode 设置 GIF** | 30 秒，三步操作。消解安装摩擦。 |
| 4 | **OG image (1200×630)** | 官网和 Reddit 分享时的预览图 |
| 5 | YouTube 短版（60 秒） | 三件事连续演示 + 结尾 CTA |

### Week 2 — 阵地（让流量有地方落）

| # | 动作 | 说明 |
|---|---|---|
| 1 | **建 Discord** | 频道：welcome / support / showcase / modpacks。挂 comparison 页链接。 |
| 2 | README 微调 | Discord 徽章 + 账号安全强信号 + Dev Mode callout 升级 |
| 3 | comparison 页 SEO | title: "Polymerium vs Prism Launcher — Switch Loaders · Snapshots · In-Place Updates"；og:image；JSON-LD |
| 4 | 注册 Reddit/YouTube/VK 账号，**潜伏 1 周** | 别急着发广告。在 r/feedthebeast 回答问题攒 karma。 |

### Week 3 — 引爆

| # | 动作 | 说明 |
|---|---|---|
| 1 | **r/feedthebeast 发帖** | 个人痛点故事开场，附核心演示 GIF。禁止广告腔。详见 §4.1 |
| 2 | **YouTube 外联** | 找 10 个 5k–50k 订阅的 modpack 评测作者。给独家选题 + ready-to-use 整合包。 |
| 3 | **非英语社区同步** | VK 俄语 MC 群 / 巴西 MC 论坛 / 印尼 Discord。用当地语言或英文发帖，大方提 Authlib 兼容性。 |
| 4 | 所有投放链接指向 comparison 页 | 让 SEO 反吃流量 |

---

## 4. 渠道打法

### 4.1 英语正版圈（主战场）

**Reddit r/feedthebeast**（最核心）：

不要发广告。发**个人故事**。

候选标题：

- *"I wanted to switch from Forge to NeoForge without rebuilding my whole instance. So I built a launcher that can."*
- *"Tired of re-importing modpacks every update? Here's how I do in-place updates with snapshots as safety net."*
- *"Other launchers freeze your instance the moment you create it. Here's why—and what happens when an instance is a recipe, not a pile of copied files."*

正文结构：痛点（具体场景 + 真实数据）→ 发现过程 → 对比 GIF → 结尾一句 "by the way, this is open source" → 链接 comparison 页。

**YouTube**：

- 找中小作者（5k–50k 订阅），不找头部（头部不理人）
- 私信要点：独家选题 + ready-to-use 整合包降低对方成本 + 强调 MIT 开源
- 一个 5 万订阅作者的视频顶 100 条 Reddit 帖

**Modrinth / CurseForge 讨论区**：

- 以"分享经验"姿态出现，不发广告
- 回答"怎么管理多个 modpack"类问题时自然带出

### 4.2 非英语平行市场（蓝海）

这批市场的人已经在用 PollyMC/FjordLauncher——他们不需要被说服"去 DRM 是好的"，他们需要的是**比 PollyMC 更好用的工具**。

**阵地列表**：

| 市场 | 核心阵地 | 语言策略 |
|---|---|---|
| 俄罗斯 | VK MC 群、Telegram 频道 | 英语贴 + 俄语关键句 |
| 巴西 | fórum Minecraft Brasil、Discord | 英语贴 + 葡语关键句 |
| 东南亚 | 印尼/菲律宾 MC Discord | 英语即可 |
| 印度 | r/IndianGaming、本地 Discord | 英语为主 |

**话术**（与非英语市场不同，不用藏 Authlib）：

> "Polymerium supports Ely.by and Yggdrasil-compatible auth servers—ideal for private servers, LAN parties, and community setups. Plus: switch loaders in seconds, update modpacks in place, roll back anything with snapshots."

**不主打"离线模式"**，主打"私有服务器生态兼容"。这不是文字游戏——这是对真实使用场景的准确描述。巴西的家庭服务器、俄罗斯的局域网联机、东南亚的社区服，都是合法场景。

### 4.3 社区增长飞轮（零成本引擎）

核心飞轮：

> modpack 作者用 Polymerium 秒切加载器 + 快照调试 + Git 管理发布 → 在 modpack README 里写"推荐用 Polymerium 打开" → 玩家跟着来。

所以**早期重点攻克 modpack 作者，不是散客玩家**。

物料：一份 `modpack-dev-with-polymerium` 英文指南，教 modpack 作者如何：
- 用 Git 管理 modpack 版本
- 用快照 Diff 调试 mod 冲突
- 秒切加载器测试兼容性
- 一键发布

这份指南比 10 条 Reddit 帖子更值钱。一个 modpack 作者一句推荐顶 100 条广告。

---

## 5. 物料清单

### 我可以直接做的

- [ ] README 微调：Discord 徽章、账号安全信号、Dev Mode callout 升级
- [ ] comparison 页 SEO + JSON-LD + og:image 设计指令
- [ ] Reddit 帖文案包（3 篇完整草稿）
- [ ] YouTube 外联私信模板
- [ ] `modpack-dev-with-polymerium` 英文指南
- [ ] 不同市场的社媒帖文案包（英语正版圈 + 非英语平行市场各一套）
- [ ] OG image / social card 设计指令

### 需要你做的

- [ ] 3 条核心 GIF/视频录屏
- [ ] Discord 服务器创建
- [ ] Reddit/YouTube/VK 账号注册 + 潜伏攒 karma
- [ ] YouTube 作者外联发送
- [ ] 社群发帖

---

## 6. 指标

| 指标 | 当前（估） | 1 月 | 3 月 |
|---|---|---|---|
| GitHub Stars | ~100 | 150 | 300 |
| Discord 成员 | 0 | 50 | 200 |
| comparison 页月浏览 | 低 | 300 | 2,000 |
| YouTube 合作视频 | 0 | 1 | 3 |
| 非英语社区触达帖子 | 0 | 3 | 10 |

---

## 7. 风险

| 风险 | 应对 |
|---|---|
| 英语正版圈贴"cracked launcher"标签 | \$2.1 全套处理；不在英语圈主动提 Authlib；如果有人挖出来质疑，坦荡回应"支持私有服务器生态，Microsoft OAuth 一等公民" |
| Windows Dev Mode 安装流失 | \$2.2 全套处理；15 秒 GIF + 权衡话术 |
| Reddit 被 ban | 潜伏先攒 karma；帖子以个人故事开场，不挂广告腔 |
| YouTube 作者不回复 | 列 10 个，预期回复率 20%；提供 ready-to-use 素材降低对方成本 |
| Prism 社区敌意 | comparison 页诚实列出 Prism 优势；话术："不同用例选不同工具" |
| Authlib 成为争议焦点 | **这是好事**——争议带来流量，而 Polymerium 什么都没做错（MIT 开源 + 功能兼容）。被讨论不可怕，没人讨论才可怕。 |

---

## 8. 文案原则（继承自 V2，精简版）

### 不允许做的事

- ❌ 每个 heading 带 emoji
- ❌ 完美的三段式 bullet 结构
- ❌ "next-generation" "revolutionary" "seamlessly" "game-changing" "beautiful"
- ❌ "Made with ❤️"
- ❌ 自夸"三代演进"叙事
- ❌ 把 Authlib 作为卖点宣传（英语圈）

### 必须做的事

- ✅ 用用户看到的结果写，不用空洞形容词，也不用实现细节：讲 "Switch loaders. Keep your mods." 不讲 "Instant Switching"（空洞）也不讲 "改一行 JSON 换加载器"（吓人）
- ✅ 允许句子不完整、不完美、有棱角
- ✅ 诚实列出短板——竞品比你好的地方
- ✅ 信息密度 > 形容词密度
- ✅ "不同用例选不同工具" 优于 "比 Prism 更好"

---

## 附录：PollyMC 的启示

PollyMC 不是竞品，是**教材**：

1. **不要替用户做道德判断。** Prism 加 DRM → PollyMC fork。工具应该服务用户，不是管教用户。
2. **移除功能 = 制造 fork 需求。** CurseForge/FTB 下载被 Prism 移除 → PollyMC 恢复 → 用户流向 fork。
3. **离线模式不是盗版。** 差网络、局域网联机、学校机房——这些都是合法场景。Prism 的"先正版后离线"设计排斥了这些场景。
4. **第三方验证服务器有巨大的合法市场。** 巴西的家庭服、俄罗斯的社区服、学校的局域网——Ely.by 和 Blessing Skin 不是 cracker 工具，是服务器基础设施。
5. **Polymerium 天生站对了位置。** 你没加 DRM。这个事实本身就可以变成叙事资产。不用学 PollyMC，你已经比它架构上领先一代。
