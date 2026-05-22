# Polymerium 推广视频脚本

## 方向判断

第二版脚本采用 **实机功能演示为主，抽象动画辅助解释** 的方案。

原因：一分钟视频里，用户不只需要知道 Polymerium 的架构更先进，更需要立刻看到它和其他 Minecraft 启动器的不同点。真正能打动用户的不是“元数据驱动”这个技术描述，而是这些别人没有或经常忽视的工作流能力：

- 添加内容只需要点三下鼠标。
- 用 tagging 和 rules 管理包，而不是只靠文件夹和列表。
- 更新实例的加载器、模组，并用历史记录追踪问题来源。
- 用快照辅助整合包调试和开发，允许大胆试错、随时反悔。

推荐比例：

| 类型 | 占比 | 用途 |
| --- | --- | --- |
| 实机演示 | 60% | 展示真正区别于同类软件的杀手级工作流 |
| 抽象动画 | 25% | 解释杂乱 mods 目录如何被 Polymerium 的管理模型替代 |
| 品牌包装 | 15% | Logo、标语、转场、下载引导 |

最终观感应该是：开头用动画抓住痛点，中段用实机证明“这真的更省事”，后段用历史记录和快照建立“可以大胆调试、允许反悔”的高级用户心智。

## 视频规格

- 主版本：60 秒，16:9，英文旁白，中文字幕可选
- 短版：30 秒，9:16，用于 Shorts / TikTok / B站竖屏
- 风格：快、干净、技术感、强调工作流效率
- 关键词：three clicks、tags and rules、safe updates、history、snapshots、debuggable modpacks

## 核心主张

Polymerium is not just another Minecraft launcher. It is a safer, faster workflow for building and debugging modded Minecraft instances.

中文含义：Polymerium 不只是另一个 Minecraft 启动器，而是一套更快、更安全、允许反悔的整合包管理与调试工作流。

## 60 秒主脚本

### 0-6s：混乱开场

画面类型：Motion Canvas / Remotion 动画

画面：黑色背景中出现多个文件夹：`mods`、`mods - copy`、`1.20.1 fabric`、`test pack final`、`real final`、`backup before update`。文件图标不断复制、堆叠，占满屏幕。一个玩家试图拖拽 jar 文件，随后出现冲突、重复、版本不匹配的警告标签。

旁白：

> Modded Minecraft gets messy fast.

屏幕文字：

> Too many files. Too many guesses.

音效：文件复制声、错误提示音、低频压力音。

### 6-11s：Polymerium 转场

画面类型：Motion Canvas 动画 + 品牌包装

画面：杂乱文件夹被吸入一个干净的 Polymerium 窗口。文件堆不再作为主视觉，转为实例、包、标签、规则、历史记录这些结构化对象。

旁白：

> Polymerium turns that chaos into a workflow.

屏幕文字：

> Not file piles. A workflow.

### 11-23s：杀手级功能一，三下添加内容

画面类型：真实录屏 + Remotion 鼠标点击标注

画面：实机展示添加内容流程。镜头要清楚显示三次点击：选择内容、添加到实例、部署或应用。每次点击时出现大号数字标注 `1`、`2`、`3`。鼠标轨迹可以用 Remotion 增强，但 UI 必须是真实录屏。

旁白：

> Add content in three clicks. Find it, add it, deploy it.

屏幕文字：

> Find. Add. Deploy.

备注：这是全片第一个杀手级功能，必须实机展示，不能只用动画替代。

### 23-34s：杀手级功能二，tagging 和 rules 管理包

画面类型：真实录屏为主 + Motion Canvas 辅助图解

画面：实机展示包或内容被打上标签，例如 `performance`、`client-only`、`server-required`、`optional`、`testing`。随后切到抽象图解：标签和规则决定哪些内容进入哪个实例或部署环境，而不是用户手动记忆每个 jar 应该放在哪里。

旁白：

> Tags and rules let packages describe how they should behave, instead of forcing you to remember where every file belongs.

屏幕文字：

> Tags describe intent. Rules apply it.

备注：如果当前 UI 还没有完全适合展示的 rules 画面，可以用实机截图 + Motion Canvas 图解组合表现，但不要伪造不存在的具体按钮行为。

### 34-46s：杀手级功能三，安全更新和历史记录查错

画面类型：真实录屏 + Remotion 时间线动画

画面：展示更新实例加载器或模组版本。随后右侧出现历史记录时间线：`Loader updated`、`Mod A 1.2 -> 1.3`、`Rule changed`、`Deployment failed`。某一条历史记录被高亮，镜头暗示用户可以定位“是哪次变化导致问题”。

旁白：

> Update loaders and mods without flying blind. When something breaks, history shows what changed.

屏幕文字：

> Update boldly. Debug with history.

备注：这一段要突出“查错”而不是普通更新功能。重点不是能更新，而是更新后出错可以追踪。

### 46-54s：杀手级功能四，快照辅助整合包调试

画面类型：Remotion / Motion Canvas 概念动画，可混合实机占位

画面：展示一个整合包开发者正在调试：先创建 `Snapshot A`，然后添加实验性 mod、修改规则、更新加载器。出现错误后，画面快速回退到 `Snapshot A`。最后分叉出两个实验路径：`Stable` 和 `Experiment`。

旁白：

> Snapshots make modpack development safer. Experiment, compare, and roll back when a change goes wrong.

屏幕文字：

> Experiment freely. Roll back safely.

备注：快照功能如果公开视频发布时仍未完成，应在画面中弱化为 roadmap / coming soon，或改成 “designed for snapshot-based debugging”。当前脚本可以先保留该卖点。

### 54-60s：收束与下载引导

画面类型：Remotion 品牌结尾

画面：Polymerium Logo 居中。背景不再是文件夹，而是一个干净的工作流图：`Add Content` -> `Rules` -> `Update` -> `History` -> `Snapshot` -> `Play`。最后出现 GitHub Releases / Download。

旁白：

> Polymerium. Build modded Minecraft instances with confidence.

屏幕文字：

> Polymerium
> Build. Debug. Roll back.
> Download on GitHub

## 中文旁白版本

### 0-6s

> Minecraft 整合包管理，很快就会变得混乱。

### 6-11s

> Polymerium 把这些混乱文件，变成一套清晰的工作流。

### 11-23s

> 添加内容只需要三下。找到它，添加它，然后部署它。

### 23-34s

> Tag 和 Rule 让包自己描述应该如何工作，而不是让你记住每个文件该放在哪里。

### 34-46s

> 更新加载器和模组时，不再盲猜。出了问题，历史记录会告诉你到底改了什么。

### 46-54s

> 快照让整合包开发更安全。大胆实验，对比结果，错了就回退。

### 54-60s

> Polymerium。用更有信心的方式，构建和调试 Minecraft 整合实例。

## 30 秒短版脚本

### 0-4s

画面：杂乱 mods 文件夹快速堆叠。


旁白：

> Modded Minecraft gets messy fast.

屏幕文字：

> Stop managing file piles.

### 4-12s

画面：实机三下添加内容，数字 `1`、`2`、`3` 跟随点击出现。

旁白：

> Polymerium lets you add content in three clicks.

屏幕文字：

> Find. Add. Deploy.

### 12-19s

画面：tags 和 rules 管理内容，抽象规则图辅助说明。

旁白：

> Tags and rules keep packages organized across instances.

屏幕文字：

> Tags + Rules

### 19-25s

画面：更新加载器和模组，历史记录定位问题，快照回退。

旁白：

> Update boldly, debug with history, and roll back with snapshots.

屏幕文字：

> Debug. Compare. Roll back.

### 25-30s

画面：Logo 和下载引导。

旁白：

> Polymerium. Build modded Minecraft with confidence.

屏幕文字：

> Polymerium
> Download on GitHub

## 镜头资产清单

### 必须真实录屏

- 三下添加内容：查找内容、添加内容、部署或应用。
- tagging：给包或内容添加标签。
- rules：展示规则管理入口或规则效果。
- 更新实例加载器。
- 更新模组版本。
- 历史记录查看和定位变更。
- 如果可用，展示快照创建、对比、回退。

### 可以使用抽象动画

- 杂乱 mods 目录堆叠。
- 文件堆转化为 Polymerium 工作流。
- tag 和 rule 的概念解释。
- 历史记录时间线增强。
- 快照分叉、实验、回退。

### 可直接使用的已有截图

- `assets/screenshots/overview.avif`
- `assets/screenshots/window.png`
- `assets/screenshots/setup.png`
- `assets/screenshots/instance.png`
- `assets/screenshots/packages.png`
- `assets/screenshots/modpacks.png`
- `assets/screenshots/accounts.png`

## Remotion / Motion Canvas 拆分建议

### Remotion 组件

- `PromoVideo`
- `MessyModsIntro`
- `WorkflowTransition`
- `ThreeClickAddContentDemo`
- `TagsAndRulesDemo`
- `UpdateHistoryDebuggingDemo`
- `SnapshotRollbackDemo`
- `ClosingCta`

### Motion Canvas 场景

- `messy-mods-directory`
- `file-piles-to-workflow`
- `tags-and-rules-graph`
- `history-timeline`
- `snapshot-branch-and-rollback`

## 节奏原则

- 前 6 秒只讲痛点，不讲架构。
- 11 秒前必须进入 Polymerium，不要让观众等太久。
- 三下添加内容必须用实机演示，因为这是最直观的效率卖点。
- tagging 和 rules 可以用实机加动画解释，但不要变成纯概念。
- 历史记录和快照要放在“开发容错”这个统一主题下：大胆更新、快速定位、允许反悔。
- 一分钟内不要深入解释资源池、符号链接、元数据实现细节，除非做 90 秒技术版。
- 每个屏幕文字尽量不超过 8 个英文单词。
- 旁白要强调用户收益，不要堆技术名词。

## 对外措辞风险

- 如果快照功能在视频发布时尚未完成，不建议在正式宣传片中直接说 “Snapshots are built in”。可以改成 “Snapshot-based debugging is coming” 或 “Designed for snapshot-based debugging”。
- 如果 rules 的 UI 还不完整，画面应展示概念图和已有管理能力，避免伪造具体不存在的操作流程。
- 对外宣传“三下鼠标”时，实机录屏必须能真的完成三步闭环，否则应改成 “Add content in just a few clicks”。
