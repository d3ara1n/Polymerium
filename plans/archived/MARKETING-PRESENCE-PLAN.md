# Polymerium 门面与市场影响力提升计划

> 核心判断：Polymerium 的技术竞争力充足，但 **零市场覆盖** 是当前最大瓶颈。
> 用户不懂技术，但有审美。第一印象决定一切。

---

## 0. 现状诊断

### 我们有什么（别人看不到的优势）

| 资产 | 现状 | 问题 |
|------|------|------|
| 全平台真 symlink 去重 | ✅ Windows/macOS/Linux 全部生效 | 没人知道，README 一笔带过 |
| 30+ 命令 CLI + MCP 模式 | ✅ Trident.Cli 已发布到 NuGet | 没有在任何地方宣传 |
| 快照系统 | ✅ 内容寻址备份、diff 恢复 | 没有任何截图或演示 |
| 双语（中/英） | ✅ 已实现 | 文档只有中文 Features.md |
| 4 种模组加载器 | ✅ Forge/NeoForge/Fabric/Quilt | 和竞品打平，不算差异 |
| 多格式导入导出 | ✅ Modrinth + CurseForge + MultiMC | 没有文档说明 |
| OOBE 引导流程 | ✅ 新手向导 | 没有截图展示 |
| 整合包版本控制 | ✅ 元数据 = 便携 JSON，Git 友好 | 没有教程或案例 |
| Sentry 错误上报 | ✅ 生产环境自动收集 | 用户不知道有售后 |
| Mirror酱 CDN | ✅ 中国用户高速下载 | README 仅一行链接 |
| 推广视频脚本 | ✅ `plans/PROMO-VIDEO-SCRIPT.md` 已写好 | 未制作 |

### 我们缺什么（用户能看到的差距）

| 维度 | Shard 做了 | 我们没做 |
|------|-----------|---------|
| Landing Page | `shard.thomas.md` — Next.js 专业暗色站 | 无 |
| 动态 Hero 文案 | 4 条循环痛点标语 | 一句静态描述 |
| macOS 风格截图轮播 | 4 张自动轮播 + 窗口 chrome 模拟 | 1 张 avif + 11 张散落 PNG |
| 3 个 CTA 按钮 | Download / Install CLI / GitHub | 文本链接 |
| 文档站（12 页） | 概念解释、安装指南、CLI 参考、FAQ | Features.md（仅中文） |
| 设计系统 | 统一暗色调 + Geist 字体 + 毛玻璃 | GitHub 默认样式 |
| SEO | JSON-LD + 动态 OG 图 + sitemap | 无 |
| Discord 社区 | ✅ | 无 |
| 视频演示 | screenshot.webp（动图预览） | 无 |

### 竞争对手 Shard 的弱点（可利用）

1. **Windows 上不做 symlink**——实例化时始终复制文件，多个实例共存时磁盘占用和传统启动器一样
2. **没有快照系统**——无法保存/恢复游戏状态
3. **没有中文支持**——整个中国市场空缺
4. **没有离线模式**——无 OfflineAccount
5. **没有第三方验证**——无 AuthlibInjector / Yggdrasil
6. **没有整合包导出**——只支持 .mrpack 导入
7. **没有 MCP 模式**——无法让 AI Agent 管理实例

---

## 1. Phase 0：README 重写（0 成本，立即执行）

> 目标：把 GitHub README 从「开发者文档」变成「面向用户的 Landing Page」。
> 参考对象：Shard 的 README 和 Landing Page 文案。

### 1.1 Hero 区

**现有文案：**
> A next-generation Minecraft instance manager that thinks differently about game management.

**问题：** "next-generation" 和 "thinks differently" 是空洞的自夸，没有传达具体价值。

**新文案（待确认）：**

| 候选 | 英文 | 中文 |
|------|------|------|
| A（痛点直击） | Your SSD called. It wants its 50GB of duplicate mods back. | 你的 SSD 上有 5 份 Sodium。Polymerium 只存 1 份。 |
| B（结果导向） | The Minecraft launcher that stores each mod exactly once. | 每个模组只存一份的 Minecraft 启动器。 |
| C（对比触发） | Prism copies. Shard copies (on Windows). Polymerium links. | 别人复制，我们链接。 |

**副标题（固定）：**
> Metadata-driven instances with zero-duplication storage, cross-platform symlinks, snapshots, and a built-in CLI with MCP mode for AI agents.

### 1.2 Before/After 对比图（新增）

在 Hero 下方放置一张直观的磁盘占用对比：

```
Before (Traditional Launcher):        After (Polymerium):
┌─────────────────────────┐          ┌─────────────────────────┐
│ Instance A: 2.3 GB      │          │ Shared Cache: 2.3 GB    │
│ Instance B: 2.1 GB      │          │ Instance A:  symlinks    │
│ Instance C: 2.4 GB      │          │ Instance B:  symlinks    │
│ ─────────────────────    │          │ Instance C:  symlinks    │
│ Total: 6.8 GB           │          │ Total: 2.3 GB           │
│ (3x Sodium, 3x Iris,    │          │ (1x Sodium, 1x Iris,    │
│  3x Fabric API...)      │          │  1x Fabric API...)      │
└─────────────────────────┘          └─────────────────────────┘
```

来源：ASCII art 或设计工具制作，截图放 `assets/comparison.png`。

### 1.3 三段式痛点 → 方案 → 证明

| 区块 | 内容 | 来源 |
|------|------|------|
| **Pain** | "Got 5 modpacks? Your SSD probably has 5 copies of Sodium." | Prism #2611 Issue 原话改编 |
| **Solution** | 元数据驱动 + symlink 去重 + 按需部署 | 核心技术 |
| **Proof** | 对比图 + 特性列表 + 下载量 badge | 数据 |

### 1.4 特性卡片重新设计

**现有：** 纯文字列表，没有视觉层次。

**改为 6 张卡片（2×3 网格）：**

| 卡片 | 图标 | 标题 | 描述 |
|------|------|------|------|
| 1 | 💾 | Zero Duplication | Each mod file stored once, symlinked everywhere. Save 60-80% disk space. |
| 2 | 🔄 | Instant Switching | Change modpacks in seconds, not minutes. No file copying. |
| 3 | 📸 | Snapshots & History | Save, restore, and diff entire game states. Safe to experiment. |
| 4 | 📦 | Git-Friendly Modpacks | Instance = one JSON file. Version control your modpack development. |
| 5 | 🤖 | CLI + MCP Mode | 30+ commands for automation. Let AI agents manage your instances. |
| 6 | 🔒 | Privacy First | No ads, no telemetry, no data collection. Open source (MIT). |

### 1.5 对比表（新增，直接针对竞品）

```markdown
| Feature | Polymerium | Prism Launcher | Shard |
|---------|:----------:|:--------------:|:-----:|
| Zero-duplication storage | ✅ Symlinks (all platforms) | ❌ Copies per instance | ⚠️ Unix only (Windows copies) |
| Instance snapshots | ✅ | ❌ | ❌ |
| CLI | ✅ 30+ commands | ❌ | ✅ |
| MCP / AI Agent mode | ✅ | ❌ | ❌ |
| Offline accounts | ✅ | ✅ | ❌ |
| Authlib-injector | ✅ | ❌ | ❌ |
| Chinese localization | ✅ | ❌ | ❌ |
| Cross-platform | Win / Mac / Linux | Win / Mac / Linux | Win / Mac / Linux |
| Modrinth + CurseForge | ✅ | ✅ | ✅ |
| Modpack export | ✅ Multi-format | ✅ | ❌ |
| Open source | MIT | GPL-3.0 | MIT |
```

### 1.6 CTA 升级

**现有：** `[📥 Download](https://github.com/d3ara1n/Polymerium/releases)`

**改为三个醒目按钮：**
- **Download for [Platform]** — 自动检测 OS，显示对应按钮
- **Try the CLI** → 指向 Trident.Cli NuGet / 安装说明
- **View on GitHub** → 仓库链接

### 1.7 社区入口（新增）

在 README 底部显眼位置添加：
- Discord 链接（待创建）
- QQ 群号（待创建）
- KLPPBS 论坛帖链接

### 1.8 交付物

- [ ] 重写 `README.md`（英文）
- [ ] 重写 `README.zh.md`（中文）
- [ ] 制作 `assets/comparison.png`（磁盘占用对比图）
- [ ] 截图升级为带窗口 chrome 的专业截图（参考 Shard 风格）

---

## 2. Phase 1：Landing Page 官网

> 目标：给 Polymerium 一个独立于 GitHub 的门面。
> 原则：宁可简陋也不要没有。

### 2.1 技术选型

| 方案 | 推荐度 | 理由 |
|------|--------|------|
| **VitePress** | ⭐⭐⭐⭐⭐ | Markdown 驱动，和文档站共用，免费部署到 GitHub Pages，开箱即用的暗色主题 |
| Astro | ⭐⭐⭐⭐ | 更灵活，但需要更多前端工作 |
| Nextra | ⭐⭐⭐⭐ | Shard 用的，功能丰富但依赖 Next.js |
| 纯 HTML/CSS | ⭐⭐ | 最简单但不可扩展 |

**推荐 VitePress**：Markdown 写内容，Vue 组件做交互，和文档站无缝整合。

### 2.2 站点结构

```
polymerium-website/
├── .vitepress/
│   ├── config.ts          # 站点配置、导航、侧边栏
│   └── theme/
│       └── index.ts       # 自定义主题（品牌色、字体）
├── index.md               # Landing Page
├── download.md            # 下载页（按平台）
├── compare.md             # 对比页（vs Prism vs Shard）
├── cli.md                 # CLI & MCP 文档
├── docs/
│   ├── getting-started.md
│   ├── creating-instance.md
│   ├── adding-mods.md
│   ├── deploying.md
│   ├── snapshots.md
│   ├── import-export.md
│   ├── configuration.md
│   └── faq.md
└── public/
    ├── screenshots/       # 高质量截图
    ├── comparison.png     # Before/After 图
    └── og-image.png       # 社交分享图
```

### 2.3 Landing Page (`index.md`) 内容规划

**Section 1: Hero（首屏）**

- 动态标语（可选，或固定一条最有力的）
- 副标题：一句话价值主张
- 3 个 CTA 按钮
- 首屏截图（最漂亮的一张，带窗口 chrome）

**Section 2: Pain Point（痛点）**

> "Most launchers copy every mod into every instance. Got 5 modpacks? That's 5 copies of Sodium on your disk."
>
> — 一个 Prism 用户的 Issue 原话

**Section 3: How It Works（3 步解释）**

1. **Describe** — 你的游戏设置是一个轻量元数据文件
2. **Deploy** — Polymerium 从共享缓存构建实例（symlink，不复制）
3. **Play** — 启动游戏。想切换？秒切。

配合简洁的动画或示意图。

**Section 4: Feature Cards（6 张）**

与 README 1.4 相同的 6 张卡片，但带交互效果。

**Section 5: Comparison Table**

与 README 1.5 相同的对比表。

**Section 6: CLI & MCP（独特卖点）**

> Polymerium comes with a 30+ command CLI and an MCP server mode.
> Let AI agents manage your Minecraft instances.
>
> ```bash
> trident instance create my-pack --version 1.21.4 --loader fabric
> trident package add modrinth:sodium
> trident instance build my-pack
> trident instance run my-pack
> ```

**Section 7: Testimonials / Community**

- KLPPBS 用户评价（待收集）
- GitHub Stars 增长图
- Discord / QQ 群入口

**Section 8: Footer**

- 下载链接
- GitHub / Discord / QQ
- MIT License
- "Made with ❤️"

### 2.4 设计系统

| 令牌 | 值 | 用途 |
|------|-----|------|
| 主色调 | `#7C3AED`（紫色）或自定义 | 按钮、链接、强调 |
| 背景 | `#0F0F11`（深色） | 页面背景 |
| 卡片背景 | `#1A1A1E` | 特性卡片 |
| 文字 | `#F5F5F5` | 主文本 |
| 次要文字 | `#9CA3AF` | 描述文本 |
| 字体 | Inter 或系统默认 | 正文 |
| 代码字体 | JetBrains Mono | 代码块 |

> 注：主色调需要根据 Polymerium 现有品牌色确定。如果应用本身有 accent color，保持一致。

### 2.5 部署

- **GitHub Pages**（免费）：`polymerium-website` 仓库或 `gh-pages` 分支
- **自定义域名**（可选）：`polymerium.cc` 或 `polymerium.dev`
- **CI/CD**：GitHub Actions 自动构建部署

### 2.6 SEO 基础

- `robots.txt`
- `sitemap.xml`（VitePress 自动生成）
- OG 图片（社交分享预览）
- 页面 title / description meta
- JSON-LD SoftwareApplication 结构化数据
- Google Search Console 提交

### 2.7 交付物

- [ ] 初始化 VitePress 项目
- [ ] 编写 Landing Page 内容
- [ ] 配置品牌主题（色彩、字体）
- [ ] 制作高质量截图（至少 4 张，带窗口 chrome）
- [ ] 配置 GitHub Pages 部署
- [ ] 配置自定义域名（可选）
- [ ] 添加 SEO 基础设施
- [ ] 中英双语版本

---

## 3. Phase 2：文档站

> 目标：让新用户 5 分钟内跑起来，让老用户找到进阶功能。
> 与 Landing Page 共用 VitePress 实例。

### 3.1 文档内容清单

#### 入门（Getting Started）

| 文档 | 内容 | 状态 |
|------|------|------|
| 安装与下载 | 各平台安装步骤、Developer Mode 说明 | 需编写 |
| 创建第一个实例 | 版本选择、加载器选择、实例命名 | 需编写 |
| 添加模组 | CurseForge / Modrinth 浏览、搜索、安装 | 需编写 |
| 部署与启动 | Deploy 操作解释、Launch 流程 | 需编写 |

#### 概念（Concepts）

| 文档 | 内容 | 状态 |
|------|------|------|
| 元数据驱动架构 | 什么是 Profile、Rice、Entry、Purl | 需编写 |
| 资源去重原理 | Symlink 如何工作、缓存结构、为什么节省空间 | 需编写 |
| 部署管线 | 7 阶段 DeployEngine 简述 | 需编写 |
| 分层配置 | persist/、live/、build/ 目录解释 | 需编写 |
| 规则系统（Rules） | Tag、Filter、Skipping、Destination | 需编写 |

#### 进阶（Advanced）

| 文档 | 内容 | 状态 |
|------|------|------|
| 快照系统 | 保存/恢复游戏状态、diff 对比 | 需编写 |
| 整合包导入导出 | 支持的格式、操作步骤 | 需编写 |
| 整合包开发与 Git | 如何用 Git 协作开发整合包 | 需编写 |
| 账户管理 | 微软账户、离线账户、Authlib-injector | 需编写 |
| 配置项一览 | Configuration 所有设置项说明 | 需编写 |
| CLI 使用指南 | `trident` 命令行工具完整参考 | 需编写 |
| MCP 模式 | 如何用 AI Agent 管理 Minecraft 实例 | 需编写 |

#### FAQ

| 问题 | 回答要点 |
|------|---------|
| 为什么要开启 Developer Mode？ | Symlink 权限需要。Windows 11 中一步设置。不影响系统安全。 |
| 和 Prism Launcher 有什么区别？ | 去重存储、快照、CLI、MCP、中文支持 |
| 和 Shard 有什么区别？ | Windows 真去重、快照、导入导出、中文支持 |
| 我的数据存在哪里？ | 本地 SQLite + JSON 文件，不上传任何数据 |
| 可以从 Prism/MultiMC 迁移吗？ | 导入整合包即可迁移 |
| 支持哪些模组加载器？ | Forge、NeoForge、Fabric、Quilt |
| 免费/开源吗？ | MIT 开源，永久免费 |

### 3.2 文档写作原则

1. **面向用户**，不面向开发者——不说 "DeployEngine state machine"，说 "点击部署，Polymerium 会自动处理一切"
2. **每页配截图**——不是文字描述，而是"看到就懂"
3. **中英双语**——同一内容维护两个版本
4. **示例驱动**——不是抽象概念，而是"从零创建一个 Fabric 1.21.4 模组包"

### 3.3 交付物

- [ ] 编写 4 篇入门文档（中英双语）
- [ ] 编写 5 篇概念文档（中英双语）
- [ ] 编写 7 篇进阶文档（中英双语）
- [ ] 编写 FAQ（中英双语）
- [ ] 每篇配截图
- [ ] 集成到 VitePress 站点

---

## 4. Phase 3：截图与视觉资产升级

> 目标：让用户一眼觉得"这个软件很专业"。

### 4.1 截图规范

| 要求 | 说明 |
|------|------|
| 分辨率 | 至少 1920×1080，推荐 2560×1440 |
| 格式 | `.webp`（主用），`.png`（fallback） |
| 窗口 chrome | macOS 风格（三色按钮）或 Windows 11 风格 |
| 内容 | 展示真实数据（真实的模组列表、真实的设置界面） |
| 暗色主题 | 默认展示暗色主题 |
| 无个人隐私 | 不展示真实用户名、UUID、Token |

### 4.2 必须截图清单

| 编号 | 截图 | 用途 |
|------|------|------|
| S1 | 主界面（实例列表 + 侧边栏） | Hero 首屏 |
| S2 | 实例详情（模组列表 + 搜索） | 功能展示 |
| S3 | 部署过程（进度条 + 状态） | 工作流展示 |
| S4 | CurseForge / Modrinth 商店浏览 | 仓库集成 |
| S5 | 快照管理界面 | 独特功能 |
| S6 | 设置页面 | 功能完整性 |
| S7 | OOBE 引导流程 | 新手友好 |
| S8 | 导入/导出整合包 | 工作流 |
| S9 | 账户管理 | 功能 |
| S10 | Windows/Linux/macOS 对比 | 跨平台 |

### 4.3 截图自动化（可选）

创建 `scripts/capture-screenshots.sh`（参考 Shard 的同名脚本）：
- 使用 Avalonia 的截图 API 或系统截图工具
- 自动化 CI 中生成
- 确保每次 UI 变更后截图都是最新的

### 4.4 其他视觉资产

| 资产 | 说明 |
|------|------|
| Logo SVG | 高分辨率矢量 Logo |
| OG Image | 1200×630 社交分享图 |
| App Icon | 各平台图标（ico、icns、png） |
| 演示 GIF | 10-15 秒循环的 Deploy → Launch 过程 |
| Before/After 图 | 磁盘占用对比（参考 1.2） |

### 4.5 交付物

- [ ] 制作 10 张规范截图
- [ ] 制作 OG Image
- [ ] 制作演示 GIF
- [ ] 制作 Before/After 对比图
- [ ] 截图自动化脚本（可选）

---

## 5. Phase 4：社区基础设施

> 目标：给用户一个留下来的理由——有人可以交流。

### 5.1 Discord 服务器（国际社区）

**频道结构建议：**

```
📍 Welcome
  ├── #welcome         — 规则、介绍
  ├── #announcements   — 版本发布、重要通知
📦 Help & Support
  ├── #help            — 使用问题
  ├── #bug-reports     — 指向 GitHub Issues
  ├── #feature-ideas   — 指向 GitHub Issues
💬 Community
  ├── #general         — 日常闲聊
  ├── #modpack-sharing — 分享模组包配置
  ├── #showcase        — 展示截图、作品
🔧 Development
  ├── #development     — 开发讨论
  ├── #contributors    — 贡献者交流
  ├── #i18n            — 翻译协作
🤖 CLI & MCP
  ├── #cli             — 命令行使用
  ├── #mcp-agents      — AI Agent 集成玩法
```

### 5.2 QQ 群（中国社区）

- 创建 QQ 群
- 在 KLPPBS 帖子中更新群号
- 在 README 中文版显眼位置放置

### 5.3 社区运营要点

| 动作 | 频率 |
|------|------|
| 版本发布公告（Discord + GitHub Release） | 每次发版 |
| 精选 Issue 讨论（Discord 转发） | 每周 |
| 用户反馈收集 | 持续 |
| 整合包推荐/分享 | 不定期 |

### 5.4 交付物

- [ ] 创建 Discord 服务器
- [ ] 创建 QQ 群
- [ ] 在 README 和官网添加入口
- [ ] 编写 Discord 频道说明
- [ ] 更新 KLPPBS 论坛帖

---

## 6. Phase 5：CLI & MCP 宣传

> 目标：把"30+ 命令 CLI + AI Agent MCP 模式"这个独特卖点打出去。
> 这是 Shard 完全没有的能力。

### 6.1 CLI 宣传文案

**核心信息：**
> Polymerium 不只是 GUI。它自带 `trident` 命令行工具——30+ 命令覆盖实例创建、模组管理、部署、导出全流程。CI/CD 友好，脚本可编排。

**使用场景举例：**

```bash
# 快速创建并部署一个 Fabric 实例
trident create my-modpack --version 1.21.4 --loader fabric
trident add modrinth:sodium
trident add modrinth:iris
trident build my-modpack
trident run my-modpack

# CI/CD：自动构建并发布整合包
trident export my-modpack --format modrinth --output ./releases/

# 管道操作：搜索并批量安装
trident package search "performance" --json | jq '.[].purl' | xargs -I{} trident package add {}
```

### 6.2 MCP 模式宣传文案

**核心信息：**
> `trident --mcp` 启动 MCP 服务器模式，让 AI Agent（Claude、GPT、Cursor 等）直接管理你的 Minecraft 实例。30+ 个 MCP Tool 覆盖实例、模组、加载器、账户、仓库管理。

**使用场景：**
- "帮我创建一个 1.21.4 Fabric 实例，装上性能优化模组"
- "给这个实例拍个快照，然后更新 Sodium 到最新版"
- "把这个实例导出为 Modrinth 格式"

**这是独此一家的能力**——没有其他 Minecraft 启动器支持 AI Agent 集成。

### 6.3 推广渠道

| 渠道 | 内容 |
|------|------|
| README 新增 CLI & MCP 区块 | 命令示例 + MCP 示例 |
| 文档站 CLI 专区 | 完整命令参考 |
| Landing Page 独立 Section | CLI + MCP 展示 |
| Hacker News / Reddit / r/Minecraft | "Minecraft launcher with AI Agent integration" |
| B 站 / YouTube | MCP 模式演示视频 |

### 6.4 交付物

- [ ] README 新增 CLI & MCP 区块
- [ ] 文档站 CLI 参考页
- [ ] 文档站 MCP 使用指南
- [ ] Landing Page CLI + MCP Section

---

## 7. Phase 6：视频演示

> 目标：已有脚本 `plans/PROMO-VIDEO-SCRIPT.md`，需要制作。

### 7.1 视频清单

| 视频 | 时长 | 平台 | 内容 |
|------|------|------|------|
| 主推视频 | 60 秒 | YouTube / B 站 | 完整功能演示（参考已有脚本） |
| 短视频 | 30 秒 | Shorts / TikTok / 抖音 | 核心卖点快剪 |
| CLI 演示 | 45 秒 | YouTube / Twitter | 命令行操作全流程 |
| MCP 演示 | 60 秒 | YouTube / Twitter | AI Agent 管理实例 |
| vs Prism 对比 | 90 秒 | YouTube / B 站 | 磁盘占用对比实测 |

### 7.2 制作要点

- **开头 3 秒抓人**：磁盘空间数字从 47GB 变 8GB
- **实机演示为主**（60%），抽象动画辅助（25%），品牌包装（15%）
- **英文旁白 + 中英字幕**
- **暗色主题展示**

### 7.3 交付物

- [ ] 60 秒主推视频
- [ ] 30 秒短视频
- [ ] CLI 演示视频
- [ ] MCP 演示视频
- [ ] 嵌入 Landing Page 和 README

---

## 8. Phase 7：中国市场专项

> 目标：中国 Minecraft 社区是天然优势，Shard 完全没有中文支持。

### 8.1 渠道

| 渠道 | 行动 |
|------|------|
| KLPPBS 苦力怕论坛 | 更新已有帖子，添加新截图、对比说明 |
| B 站 | 发布演示视频、MCP 演示 |
| 哔哩哔哩模组区 | 整合包推荐 + Polymerium 标签 |
| QQ 群 | 运营用户群 |
| Mirror酱 | 持续更新，确保下载可用 |
| 知乎 | 写一篇"为什么我做了一个新 Minecraft 启动器" |
| 掘金 / SegmentFault | 技术向文章：元数据驱动的启动器架构 |

### 8.2 中文内容

- [ ] Landing Page 中文版
- [ ] 文档站全量中文
- [ ] README 中文版（更新为新的文案结构）
- [ ] B 站视频（中文配音或字幕）
- [ ] 知乎文章
- [ ] KLPPBS 帖子更新

---

## 9. Phase 8：SEO 与增长

> 目标：让搜索引擎和社交平台把用户带过来。

### 9.1 SEO 基础

| 项目 | 实施 |
|------|------|
| Google Search Console | 提交站点地图 |
| 关键词 | "minecraft launcher", "minecraft instance manager", "minecraft deduplicate mods", "minecraft symlink launcher", "元数据驱动 minecraft 启动器" |
| 页面 Meta | 每页独立的 title + description |
| JSON-LD | SoftwareApplication 结构化数据 |
| OG Image | 社交分享预览图 |
| sitemap.xml | VitePress 自动生成 |

### 9.2 外链建设

| 来源 | 方式 |
|------|------|
| GitHub README | 指向官网 |
| Discord 个人资料 | 指向官网 |
| Reddit 帖子 | 指向官网 |
| KLPPBS 帖子 | 指向官网 |
| Awesome lists（Avalonia、Minecraft） | PR 添加 |
| Hacker News Show HN | 发布公告 |
| Product Hunt | 发布公告 |

### 9.3 增长指标

| 指标 | 当前 | 3 个月目标 | 6 个月目标 |
|------|------|-----------|-----------|
| GitHub Stars | 94 | 300 | 800 |
| Discord 成员 | 0 | 100 | 500 |
| 官网月访问 | 0 | 2,000 | 10,000 |
| 下载量/月 | 未知 | 追踪 | 追踪 |
| 文档页浏览 | 0 | 追踪 | 追踪 |

### 9.4 分析工具

| 工具 | 用途 |
|------|------|
| GitHub Insights | Stars、Forks、Traffic |
| Vercel Analytics 或 Umami | 官网访问量（隐私友好） |
| Discord Insights | 社区活跃度 |

---

## 10. 执行路线图

### 时间线总览

```
Week 1-2:  Phase 0 — README 重写（零成本，立即执行）
Week 2-3:  Phase 3 — 截图与视觉资产制作
Week 3-5:  Phase 1 — Landing Page 搭建
Week 4-6:  Phase 4 — 社区基础设施（Discord + QQ）
Week 5-8:  Phase 2 — 文档站内容编写
Week 6-7:  Phase 5 — CLI & MCP 宣传物料
Week 7-9:  Phase 6 — 视频制作
Week 6+:   Phase 7 — 中国市场专项（持续）
Week 4+:   Phase 8 — SEO 与增长（持续）
```

### 优先级排序

```
┌─────────────────────────────────────────────────────────┐
│  P0 (立即)     Phase 0: README 重写                      │
│  P0 (立即)     Phase 4: Discord + QQ 群                   │
├─────────────────────────────────────────────────────────┤
│  P1 (本月)     Phase 3: 截图与视觉资产                     │
│  P1 (本月)     Phase 1: Landing Page                      │
├─────────────────────────────────────────────────────────┤
│  P2 (下月)     Phase 2: 文档站                             │
│  P2 (下月)     Phase 5: CLI & MCP 宣传                    │
├─────────────────────────────────────────────────────────┤
│  P3 (持续)     Phase 6: 视频制作                           │
│  P3 (持续)     Phase 7: 中国市场                           │
│  P3 (持续)     Phase 8: SEO 与增长                         │
└─────────────────────────────────────────────────────────┘
```

---

## 附录 A：Shard Landing Page 结构参考

Shard 的 Landing Page (`shard.thomas.md`) 结构：

```
1. Hero 区
   - 动态标语（4 条循环，每 3 秒）
     ① "Shard Launcher"
     ② "Tired of 50GB of duplicate mods?"
     ③ "One library. Infinite profiles."
     ④ "Open source · Rust + Tauri"
   - 3 个 CTA：Download Desktop / Install CLI / GitHub
   - macOS 窗口截图轮播（4 张，每 5 秒）

2. 特性区（6 张卡片）
   - Save Disk Space
   - Reproducible Profiles
   - Fast & Lightweight
   - No Hidden State
   - CLI-First
   - Open Source

3. Footer
   - © 2026 Thomas Marchand
   - GitHub 链接
```

Shard 的文档站（Nextra，12 页）：
- Getting Started / First Profile / Accounts
- Concepts: Profiles / Content Store / Instances
- CLI Reference
- Building / Development / Contributing
- FAQ

### 设计系统参考

| 令牌 | Shard 值 | 用途 |
|------|---------|------|
| accent | `#e8a855`（琥珀色） | 主色调 |
| background | `#0c0b0a` | 页面背景 |
| background-elevated | `#181716` | 卡片 |
| foreground | `rgb(245, 240, 235)` | 主文本 |
| foreground-secondary | `rgb(176, 168, 159)` | 次要文本 |
| border | `rgba(255, 248, 240, 0.06)` | 边框 |
| font | Geist + Geist Mono | 字体 |

## 附录 B：Prism Launcher 用户痛点原话

来自 GitHub Issues 的真实用户反馈（可作为文案素材）：

> "I recently saw I almost use 25GB of data for Prism Launcher."
> — [#1052](https://github.com/PrismLauncher/PrismLauncher/issues/1052)

> "Utilize Central Mods Folder and Symlinks to Cut Down on Mods Across Multiple Modpacks Eating Up Storage"
> — [#2611](https://github.com/PrismLauncher/PrismLauncher/issues/2611)

> "I want the ability to share resourcepacks and shaderpacks over multiple instances"
> — [#38](https://github.com/PrismLauncher/PrismLauncher/issues/38)

> "Same mod files are installed over and over again"
> — CurseForge Ideas (CF-I-7706)

## 附录 C：Trident CLI 命令速查

```
# 实例管理
trident instance create <name> --version <mc> --loader <loader>
trident instance list
trident instance inspect <key>
trident instance build <key>        # 部署
trident instance run <key>          # 启动
trident instance export <key>       # 导出整合包
trident instance import <file>      # 导入整合包
trident instance delete <key>

# 模组管理
trident package search <query>
trident package add <purl>          # e.g. modrinth:sodium
trident package list
trident package enable/disable <purl>
trident package version list <purl>
trident package version set <purl> <version>

# 加载器
trident loader list
trident loader set <key> --loader <lurl>

# 账户
trident account add                 # 微软 Device Code Flow
trident account list
trident account remove <uuid>

# 仓库
trident repository list
trident repository add <label> <base-url>

# MCP 模式
trident --mcp                       # 启动 MCP 服务器（stdio）

# 全局选项
--home <path>     # 指定数据目录
--json            # JSON 输出
--no-interactive  # 无交互模式
--verbose / --debug
```

## 附录 D：MCP Tool 清单（30+）

| 分类 | Tool | 说明 |
|------|------|------|
| Instance | instance_list | 列出所有实例 |
| | instance_inspect | 查看实例详情 |
| | instance_create | 创建实例 |
| | instance_unlock | 解锁导入来源绑定 |
| | instance_delete | 删除实例 |
| | instance_reset | 重置构建产物 |
| | instance_export | 导出整合包 |
| | instance_import | 导入整合包 |
| Package | package_list | 列出已安装模组 |
| | package_search | 搜索模组 |
| | package_add | 添加模组 |
| | package_inspect | 查看模组详情 |
| | package_enable / package_disable | 启用/禁用 |
| | package_dependency_list | 查看依赖 |
| | package_dependent_list | 查看反向依赖 |
| | package_version_list | 列出可用版本 |
| | package_version_set | 设置版本 |
| Loader | loader_list | 列出加载器 |
| | loader_version_list | 列出可用版本 |
| | loader_get / loader_set | 查看/设置加载器 |
| Config | config_get / config_set | 配置管理 |
| | config_unset / config_list | |
| Account | account_list | 列出账户 |
| | account_add_offline | 添加离线账户 |
| | account_remove | 删除账户 |
| Repository | repository_list | 列出仓库 |
| | repository_status | 查看状态 |
| | repository_add / repository_remove | 添加/删除仓库 |
