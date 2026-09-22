---
workflow: product-launch-video
flow: automation
storyboard: yes
message: "Polymerium 把发现内容、搭配玩法和管理实例连成一个清晰的 Minecraft 桌面体验。"
destination: bilibili
aspect: 1920x1080
language: zh-CN
length: 42s
angle: product-launch
---

## Intent

制作 Polymerium 整体软件发布片。面向 Minecraft 玩家及整合包用户，以紧凑排版、模拟 UI、连续运动及音乐节奏建立产品印象。无旁白。功能教程留到后续独立视频。

## Assets

- `assets/logo.svg` — 来自仓库 `assets/brand/logo-dark.svg` 的官方深色背景标识。
- `assets/screenshots/` — 用户后续提供的真实产品截图；仅用于独立的截图展示镜头。
- `src/Polymerium.Avalonia/Pages/` 与 `Controls/` — 仿制 UI 的结构依据，见 `frame.md`。

## Customizations

- 替换已删除的旧 `promo/` 视频项目，采用 HyperFrames + GSAP。
- 用户明确要求：软件操作和心智模型通过 HTML 模拟 UI 和场景表达，界面风格、结构与实际软件接近。
- 不把 PNG 截图中的按钮、列表切片伪装成可交互组件；用 DOM 元素实现状态变化、移动和组合。
- 真实截图作为独立展示镜头中的整体平面，以扇形展开、叠放或横向掠过参与节奏；不以阅读截图细节为目标。
- 截图可留空。预览中标识空插槽，成片前补齐；禁止把仿制界面称为真实截图。
- 场景之间由同一个视觉对象或一致运动方向衔接；不用无意义漂浮填满时间。

## Notes

### 已确定

产品、发布片用途、项目目录、技术栈、无需人工讲解或屏幕录制、仿制 UI 优先、截图独立展示及允许占位，均来自对话。

### 当前制作默认

42 秒、1920×1080、中文先行、暖黑/米白/品牌金、分镜及关键画面先供审阅，沿用已提出的方案；这些是可调整的制作默认，不代表用户已逐项批准最终文案。英文复用同一镜头结构，竖屏剪辑暂不在本轮制作范围。

本阶段交付项目、分镜和可预览的视觉样稿。配乐/SFX 与真实截图待补，静音预览不视为最终成片。最终导出前审阅。

不新增未实现的产品功能，不暗示瞬间完成下载/部署，不承诺快照覆盖全部存档，不展示无依据的性能或用户数量。
