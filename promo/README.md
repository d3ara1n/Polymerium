# Polymerium 发布视频

HyperFrames + GSAP 的 42 秒、1920×1080 中文发布片视觉草案。无旁白，当前预览静音；配乐及三张真实截图待补。

## 工具链

项目使用 npm 本地依赖中的官方 HyperFrames CLI，不探测其他项目或自动借用其他工具安装的浏览器。

| 用途 | 依赖 |
| --- | --- |
| 安装依赖、启动 Studio | Node.js 22+、npm |
| 本地检查、截图和渲染 | HyperFrames 管理的 Chrome Headless Shell |
| 本地媒体处理和视频编码 | FFmpeg / FFprobe，可从 PATH 调用 |
| 容器内最终渲染（可选） | 已安装并运行的 Docker |

本地初始化：

```sh
cd promo
npm ci
npm run browser:install
npm run doctor
npm run dev
```

`browser:install` 调用官方 `hyperframes browser ensure`，由框架解析和管理兼容的浏览器版本，首次使用会下载浏览器到框架缓存目录。它是单独的显式步骤，不是 npm 安装钩子。当前锁定的 HyperFrames 0.8.59 为现代操作系统选择 Chrome Headless Shell 152.0.7977.30；框架可能为旧系统选择兼容版本，见官方环境诊断输出。

Node.js 和 FFmpeg 的系统安装由使用者完成。`doctor` 列出的语音识别、TTS、本地音乐生成工具对于当前无旁白视频不是必需项。默认本地流程也不要求 Docker。普通浏览器只负责查看 Studio，不等同于用于渲染的受管 Headless Shell。

在 Studio 中打开 `promo` 项目。`dev` 使用前台进程，结束预览按 Ctrl+C。

## 检查和输出

```sh
npm run lint
npm run check
npm run snapshot
```

`lint` 是静态检查，不需要渲染浏览器；`check` 检查实际浏览器内的时间轴、脚本、布局和对比度；`snapshot` 生成七个关键画面与联系表。

视觉、节奏和音轨审阅后：

```sh
npm run render
```

输出为 `renders/polymerium-launch-zh.mp4`。脚本检查截图与配乐是否齐备，避免把占位视觉稿作为发布成片输出。如需有意输出静音草案，可用 `npm exec -- hyperframes render --quality draft --output renders/draft.mp4`。

可选的容器输出：

```sh
npm run render:docker
```

Docker 路径把渲染浏览器和 FFmpeg 放到官方渲染环境内，适合 CI 和最终交付。它仍需项目的 Node/npm 来调用 CLI。本地与容器结果不保证字节一致；ARM64 和 AMD64 容器结果也可能不同。需要多机器严格复现时，应统一目标架构，并固定最终渲染镜像及其摘要、浏览器、FFmpeg 和字体版本。当前项目尚未验证 Docker 输出，不将本地检查当作容器验收。

官方文档：

- https://hyperframes.heygen.com/guides/rendering
- https://hyperframes.heygen.com/packages/cli
- https://hyperframes.heygen.com/guides/troubleshooting

## 文件

- `BRIEF.md`：目标、范围和用户明确的制作原则。
- `STORYBOARD.md`：七幕文案、时间点、动作和素材来源。
- `frame.md`：品牌配色、UI 提炼尺度和运动语言。
- `index.html`：HTML 模拟 UI 与确定性的 GSAP 时间轴。
- `styles.css`：视频排版与模拟界面的样式。
- `ledger.json`：六个镜头边界的运动方向和时刻。
- `assets.json`：真实截图与配乐的路径配置。

## UI 与截图

操作、选择状态和组织关系使用独立 HTML 元素。全局侧栏在右，实例内导航在左；实例卡片的信息顺序、包管理集合与启用开关参照当前 XAML。模拟 UI 是适合视频阅读的提炼，并非逐像素复制。示例名称和标签只用于演示。

24–30 秒是独立的截图展示镜头：三张真实产品截图整体扇形展开，不拆分内部按钮或依靠放大文字讲解。未提供的图片显示明确占位框。

将截图放入 `assets/screenshots/`，然后修改 `assets.json`：

```json
{
  "screenshots": [
    {"id":"landing", "label":"首页", "src":"assets/screenshots/landing.png"},
    {"id":"setup", "label":"包管理", "src":"assets/screenshots/setup.png"},
    {"id":"instance", "label":"实例主页", "src":"assets/screenshots/instance.png"}
  ],
  "music": null
}
```

路径相对 `promo/`。建议三张截图采用一致窗口比例与主题，移除私人账号信息；约 1600px 以上宽度即可，优先保证整体构图一致。重启 `npm run dev` 会重新生成配置。截图不要求现在提供。

配乐也通过 `assets.json` 指定本地文件。目前音轨入口只做基本铺底，正式阶段仍需选曲、按音乐调整节拍、设计音效与收尾。使用拥有相应使用权的素材。

## 素材与依赖

Logo 来自 `../assets/brand/logo-dark.svg`，原样复制。GSAP 3.14.2、HyperFrames 0.8.59 和 Noto Sans SC Variable 5.3.0 均固定版本，依赖树由 `package-lock.json` 锁定。`npm ci` 将渲染用脚本和字体准备到本地，不依赖在线 CDN、系统中文字体或另一项目的浏览器缓存。字体许可见依赖包的 `LICENSE`。生成的缓存、字体副本、检查截图与视频不纳入版本控制。

当前静态检查包含单文件场景结构的拆分建议。这些提示是维护性建议，不等同于渲染错误。正式片仍需视觉审阅，自动检查不评判创意质量。
