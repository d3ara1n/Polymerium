# Polymerium 市场门面与文档计划 V2

> 基于 V1 的执行框架，V2 重新梳理了产品特色的全貌，不再局限于 symlink 去重这一个卖点。
> 目标：让用户和搜索引擎能全面了解 Polymerium 的差异化价值。

---

## 0. 产品特色全景（Feature Landscape）

以下特色按层次分类，每一项都值得在文档或 Landing Page 中体现。

### 🏗️ 架构层面

| # | 特色 | 核心描述 | 文档重点 |
|---|------|---------|---------|
| 1 | **元数据驱动架构** | 实例不是 `.minecraft` 文件夹，而是一份 `profile.json`。改版本、换加载器、增删模组都是修改元数据，再按需构建（Deploy）。因此可以随时增量构建、秒切加载器类型和版本。 | 概念文档："元数据驱动实例" |
| 2 | **四层文件隔离** `import → live → persist → build` | 每层有独立生命周期：`import` = 整合包原始文件，`live` = 运行时工作副本，`persist` = 玩家本地数据（跨更新保留），`build` = 部署产物（可随时重建）。其他启动器没有这种分离。 | 概念文档："四层目录模型" |
| 3 | **快照系统** | 基于内容寻址的实例快照，可保存/恢复/对比整个游戏状态，支持 diff 查看。大胆折腾前先拍快照。 | 专题文档："快照与回滚" |
| 4 | **规则引擎（Rules）** | 为每个模组定义部署规则：按标签/仓库/类型条件性地跳过、重定向路径、规范化文件名。例如解决不同加载器 datapack 路径差异。 | 进阶文档："部署规则" |
| 5 | **Git 友好的整合包开发** | 实例 = `profile.json` + `import/` + 图标 + README + CHANGELOG。`.gitignore` 排除 `build/`、`live/`、`persist/`，剩余即为纯净整合包源码，可用 Git 协作。 | 进阶文档："整合包开发工作流" |

### 🎮 玩家体验层面

| # | 特色 | 核心描述 | 文档重点 |
|---|------|---------|---------|
| 6 | **4 种模组加载器** | Forge、NeoForge、Fabric、Quilt 全覆盖。元数据驱动意味着随时切换加载器，不需要重新下载整个实例。 | 入门文档："创建实例" |
| 7 | **4 种账户类型** | 微软（Device Code Flow OAuth）、离线、Authlib Injector（第三方验证 / Yggdrasil）、Trial。支持**按实例绑定**不同账户。 | 进阶文档："账户管理" |
| 8 | **4 种导入导出格式** | Trident（原生）、CurseForge、Modrinth、MultiMC 全格式双向支持。 | 专题文档："导入与导出" |
| 9 | **双仓库模组浏览** | CurseForge + Modrinth 直接在启动器内搜索、浏览、安装，支持收藏夹虚拟仓库。 | 入门文档："添加模组" |
| 10 | **自动 Java 运行时下载** | 无需手动安装 Java——部署时自动从 Mojang 下载匹配的运行时，也支持手动指定。 | 入门文档："安装与配置" |
| 11 | **标签式模组管理** | 不用死板的单一分类，用标签（tags）组织模组。支持多标签筛选、批量更新排除。 | 进阶文档："标签与批量操作" |
| 12 | **游玩时间追踪** | 每次游戏记录开始/结束时间、崩溃状态，配合图表可视化展示周活跃、总时长等。 | 功能说明页 |

### 🔧 工具层面

| # | 特色 | 核心描述 | 文档重点 |
|---|------|---------|---------|
| 13 | **CLI + MCP 模式** | `trident` 命令行工具提供 30+ 命令。MCP 模式让 AI Agent（Claude、Cursor 等）直接管理实例。**其他 Minecraft 启动器完全不具备此能力。** | 专题文档："CLI 参考" + "MCP 模式" |
| 14 | **崩溃诊断 + AI 分析** | 三阶段错误分类（部署失败 / 启动失败 / 运行时崩溃），内置 mclogs 集成，可导出诊断包或 AI 分析包（Markdown 格式，可直接喂给 LLM）。 | 进阶文档："故障排查" |
| 15 | **双更新源** | GitHub Releases + Mirror酱（中国 CDN），中国用户高速下载更新。 | 入门文档："安装与配置" |

### 🎨 UI/UX 层面

| # | 特色 | 核心描述 | 文档重点 |
|---|------|---------|---------|
| 16 | **OOBE 引导流程** | 新用户首次启动有完整向导：功能介绍 → Windows 符号链接权限检测 → Java/语言快速配置 → 隐私确认。 | 入门文档提及 |
| 17 | **双语支持** | 中英文完整本地化（`en-US` + `zh-Hans`）。 | 全站双语 |
| 18 | **Widget 系统** | 每个实例可附加小组件：文本笔记、网络连通性检测等。 | 进阶文档提及 |
| 19 | **主题系统** | 亮/暗模式、自定义强调色、圆角风格、背景透明度、系统标题栏切换。 | 设置文档提及 |

---

## 1. 竞品对比矩阵

| 功能 | Polymerium | Prism Launcher | Shard |
|------|:----------:|:--------------:|:-----:|
| 去重存储 | ✅ symlink（全平台） | ❌ 逐实例复制 | ⚠️ 仅 Unix（Windows 复制） |
| 实例快照 | ✅ 保存/恢复/diff | ❌ | ❌ |
| CLI | ✅ 30+ 命令 | ❌ | ✅ |
| MCP / AI Agent 模式 | ✅ | ❌ | ❌ |
| 离线账户 | ✅ | ✅ | ❌ |
| Authlib Injector | ✅ | ❌ | ❌ |
| 中文本地化 | ✅ | ❌ | ❌ |
| 跨平台 | Win / Mac / Linux | Win / Mac / Linux | Win / Mac / Linux |
| CurseForge + Modrinth | ✅ | ✅ | ✅ |
| 整合包多格式导出 | ✅ 4 种格式 | ✅ | ⚠️ 仅导入 |
| 元数据驱动实例 | ✅ | ❌ | ✅ |
| 四层文件隔离 | ✅ | ❌ | ❌ |
| 标签式模组管理 | ✅ | ❌ | ❌ |
| 部署规则引擎 | ✅ | ❌ | ❌ |
| 自动 Java 下载 | ✅ | ✅ | ✅ |
| 游玩时间追踪 | ✅ 图表可视化 | ✅ 基础计时 | ❌ |
| 开源协议 | MIT | GPL-3.0 | MIT |

---

## 2. Landing Page 特性卡片设计

基于上述全景，建议 Landing Page 展示 8 张核心卡片：

| 卡片 | 图标 | 标题 | 描述 |
|------|------|------|------|
| 1 | 🧬 | Metadata-Driven | Your instance is a JSON file, not a folder copy. Change loaders, versions, and mods on the fly. |
| 2 | 💾 | Zero Duplication | Each mod file stored once, symlinked everywhere. Save 60–80% disk space. |
| 3 | 📸 | Snapshots & Rollback | Save, restore, and diff entire game states. Safe to experiment. |
| 4 | 📦 | Git-Friendly Modpacks | Instance = one JSON + assets. Version control your modpack development. |
| 5 | 🔄 | 4 Loaders, Instant Switch | Forge, NeoForge, Fabric, Quilt. Switch in seconds — no re-download needed. |
| 6 | 🤖 | CLI + MCP Mode | 30+ commands for automation. Let AI agents manage your instances. |
| 7 | 🔐 | 4 Account Types | Microsoft, Offline, Authlib Injector, Trial. Bind different accounts per instance. |
| 8 | 🌐 | Bilingual & Cross-Platform | Full English + Chinese support. Windows, macOS, Linux. |

---

## 3. 文档站点结构规划

> 技术栈：fumadocs（已搭建于 `website/` 目录）
> 现有页面：index, getting-started, creating-instance, snapshots, import-export, cli, faq
> 需扩展：概念文档、进阶文档、更多专题

### 目录结构

```
content/docs/
├── meta.json / meta.zh.json          # 侧边栏导航定义
├── index.mdx / index.zh.mdx          # 文档首页（概览 + 快速导航）
│
├── getting-started.mdx / .zh.mdx     # 安装、下载、首次配置（含 OOBE）
├── creating-instance.mdx / .zh.mdx   # 创建第一个实例
├── adding-mods.mdx / .zh.mdx         # 【新增】从 CurseForge/Modrinth 搜索安装模组
├── deploying.mdx / .zh.mdx           # 【新增】部署与启动流程（Deploy + Launch）
│
├── concepts/                         # 【新增目录】概念解释
│   ├── meta.json / meta.zh.json
│   ├── metadata-driven.mdx / .zh.mdx       # 元数据驱动架构（核心概念）
│   ├── four-layers.mdx / .zh.mdx           # 四层文件隔离模型
│   ├── deploy-pipeline.mdx / .zh.mdx       # 部署管线（8 阶段简述）
│   └── rules.mdx / .zh.mdx                 # 规则引擎（Rules）
│
├── snapshots.mdx / .zh.mdx           # 快照系统（已有）
├── import-export.mdx / .zh.mdx       # 导入与导出（已有）
│
├── advanced/                         # 【新增目录】进阶功能
│   ├── meta.json / meta.zh.json
│   ├── accounts.mdx / .zh.mdx             # 账户管理（4 种类型 + 按实例绑定）
│   ├── tags.mdx / .zh.mdx                 # 标签与批量操作
│   ├── modpack-dev.mdx / .zh.mdx          # 整合包开发与 Git 工作流
│   ├── java.mdx / .zh.mdx                 # Java 管理（手动 + 自动下载）
│   ├── widgets.mdx / .zh.mdx              # Widget 系统
│   ├── configuration.mdx / .zh.mdx        # 配置项一览
│   └── troubleshooting.mdx / .zh.mdx      # 故障排查（三阶段分类 + AI 分析）
│
├── cli.mdx / .zh.mdx                 # CLI 参考（已有）
├── mcp.mdx / .zh.mdx                 # 【新增】MCP 模式与 AI Agent 集成
│
└── faq.mdx / .zh.mdx                 # FAQ（已有，需扩充）
```

### 页面内容摘要

| 页面 | 英文标题 | 内容要点 |
|------|---------|---------|
| index | Documentation Home | 概览 + 快速导航卡片（入门 → 概念 → 进阶 → 参考） |
| getting-started | Getting Started | 下载安装（三平台）、Developer Mode 说明、OOBE 流程截图、首次启动引导 |
| creating-instance | Creating an Instance | 版本选择、加载器选择（4 种）、命名、从整合包导入创建 |
| adding-mods | Adding Mods | CurseForge/Modrinth 浏览与搜索、安装、启用/禁用、收藏 |
| deploying | Deploy & Launch | Deploy 操作解释、快速模式、三种启动模式、快速连接服务器 |
| concepts/metadata-driven | Metadata-Driven Instances | Profile → Rice → Package → Deploy 循环、为什么能秒切加载器 |
| concepts/four-layers | Four-Layer Directory Model | import/live/persist/build 的角色、生命周期、reset/update 对各层的影响 |
| concepts/deploy-pipeline | Deploy Pipeline | 8 阶段流程简述、Artifact 缓存与快速模式 |
| concepts/rules | Deployment Rules | 规则语法、选择器（Purl/Tag/Repository/Kind）、动作（Skip/Normalize/Destination） |
| snapshots | Snapshots & Rollback | 创建/恢复快照、diff 对比、使用场景 |
| import-export | Import & Export | 4 种格式、导入流程、导出选项、整合包更新 |
| advanced/accounts | Account Management | 4 种账户类型详解、按实例绑定、OAuth 刷新 |
| advanced/tags | Tags & Batch Operations | 标签 vs 分类、批量更新、排除规则 |
| advanced/modpack-dev | Modpack Development | Git 工作流、import/live 协作、导出发布 |
| advanced/java | Java Management | 手动配置 vs 自动下载、多版本支持 |
| advanced/widgets | Widget System | 笔记、网络检测、扩展机制 |
| advanced/configuration | Configuration | 所有配置项说明 |
| advanced/troubleshooting | Troubleshooting | 三阶段错误分类、诊断包导出、AI 分析包 |
| cli | CLI Reference | `trident` 命令完整参考 |
| mcp | MCP Mode | MCP 服务器启动、30+ Tool 清单、AI Agent 使用场景 |
| faq | FAQ | 常见问题（扩充版） |

---

## 4. 与 V1 计划的关系

V1 的 Phase 0–8 框架仍然有效，V2 是对内容侧的补充和修正：

- **V1 负责：** 执行节奏、技术选型、视觉资产、社区、SEO
- **V2 负责：** 特色全景、文档内容规划、Landing Page 文案方向

两者结合使用。文档编写时以 V2 的特色清单为素材库，以 V1 的执行节奏为时间线。
