# 游戏内覆盖层（in-game overlay）

## 背景

想给 Polymerium 加游戏内覆盖层：游戏运行中以热键呼出的悬浮 UI，渲染进游戏画面本体，首个预期用途是展示游戏日志等启动器侧信息。

替代方案已排除：

- 外置置顶/点击穿透窗口——MC 的全屏（GLFW 全屏窗口，改显示模式 + popup）会压住它，且 MC 没有无边框窗口选项，体验不可接受。注意 MC 的全屏并不是 D3D 那种绕过 DWM 的独占全屏，但对外置窗口同样不友好。
- mod——跨版本、跨加载器的维护成本过高，放弃。

## 做什么 / 期望效果

- 覆盖层在 java 进程内绘制，叠加在游戏帧上，全屏下可见；不改变窗口焦点（单机不失焦暂停）。
- 热键呼出/隐藏；内容由启动器侧驱动；支持多实例同时运行。
- Windows 优先，Linux/macOS 不在首期范围。

## 调研注意事项

业界三条路线，结论是走 Steam 流派的 OpenGL 特化版：

- Steam / Discord / RTSS：DLL 注入 + 图形 API Present hook，通用做法。
- NVIDIA / AMD：覆盖层实现在自家显卡驱动的用户态组件里，天然位于每个游戏的 Present 路径上——第三方没有这个立足点。
- Xbox Game Bar：DWM 合成路线，依赖系统配合，第三方只能做弱化版。

本项目场景的关键事实：

- MC Java 只走 OpenGL（LWJGL），hook 点唯一且稳定：`opengl32!wglSwapBuffers`——系统 DLL 的稳定导出，与 MC 版本、LWJGL 2/3 均无关；inline hook 对 GetProcAddress 和直接调用都生效。
- 免注入加载通道：JVM 的 `-agentpath` 启动参数（官方机制，需导出 `Agent_OnLoad`）可在启动早期把原生 DLL 载入 java 进程，agent 内自行 `LoadLibrary("opengl32.dll")` 后 hook 即可，无需等待 LWJGL。杀软摩擦远低于注入。备选的代理 `opengl32.dll` 方案仅在自管 Java 运行时目录可行。
- MC 1.17+ 使用 core profile（3.2+），固定管线不可用，UI 绘制必须走 shader——现成 UI 库（如 ImGui 的 GL3 后端）即为 core profile 实现。
- 覆盖的绘制要在默认帧缓冲上进行（MC 全程画在自家 FBO 里，swap 前状态不可假设）。
- 输入是最琐碎的部分：热键（低级键盘钩子）、打开时拦截游戏输入（MC 键盘走窗口消息、鼠标走原始输入）、光标恢复（ClipCursor / ShowCursor）。
- 覆盖的 UI 状态需按 GL 上下文隔离（hook 内用当前上下文区分）。
- 中文用户群：UI 字体必须内置 CJK 字形（常见 UI 库默认字体无中文，字体纹理会多吃几 MB）。
- 原生 DLL 用 C++ 或 Rust；不要把 .NET runtime host 进游戏进程。
- 与 RTSS / OBS 等已有覆盖共存时靠 hook 链式转发，一般兼容，但不可假设自己是链上唯一。
- 分发：未签名原生 DLL 会被 SmartScreen 盯上，基本需要代码签名。
- MC 无反作弊，注入/加载层面无封号风险。

## 参考

- Fred Emmott — In-Game Overlays: How They Work：<https://fredemmott.com/blog/2022/05/31/in-game-overlays.html>
- aixxe — Rendering with the Steam overlay：<https://aixxe.net/2017/09/steam-overlay-rendering>
- Steamworks 文档 — Steam Overlay：<https://partner.steamgames.com/doc/features/overlay>
- ReShade（crosire/reshade）——开源的 GL/D3D/Vulkan Present hook 完整实现，架构最值得参考；复用代码前核许可证
- OBS game capture hook——OpenGL hook 的生产级实现
