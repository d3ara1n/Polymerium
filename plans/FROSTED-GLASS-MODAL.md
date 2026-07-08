# Modal 毛玻璃背景

## 背景

当前 Modal 的背景是两层纯色：OverlayHost 的半透明遮罩（`OverlaySmokeBackgroundBrush`）+ Modal 自身的实心 `FlyoutBackgroundBrush`。视觉上平淡，没有 macOS 风格毛玻璃的层次感和通透感。

AppSurface 在 `:obstructed` 时已对页面内容施加了 `BlurEffect` + `scale(0.98)`，但 Modal 卡片本身仍是实心纯色，与背景没有视觉融合。

## 目标

为 Modal 卡片增加毛玻璃（frosted glass）背景效果：实时截取卡片后方内容 → 高斯模糊 → 主题色半透明叠加。15fps 刷新，让窗口移动或内容变化时 Modal 背景同步更新。

## 视觉层次

```
AppSurface（:obstructed → 已有 BlurEffect + 缩放）
└── OverlayHost (ModalHost)
    ├── Border#PART_SmokeMask ← 半透明遮罩（不变，提供基础 dimming）
    └── ItemsControl
        └── OverlayItem → Modal
            └── Border（CornerRadius + ClipToBounds + BoxShadow）
                └── FrostedGlassCard ← 🆕 毛玻璃层
                    ├── GPU Snapshot → Blur → Tint 实时渲染
                    └── ContentPresenter（文字、按钮等在上层）
```

## 技术实现

### FrostedGlassCard（核心控件）

`ContentControl`，自身不处理背景绘制——把渲染委托给自定义 `ICustomDrawOperation`：

**渲染管线（FrostedGlassCardRenderer.Render）：**

```
1. 计算控件在窗口中的裁剪区域（TransformToVisual + Bounds）
2. 检查时间闸：距离上次截取 < 66ms？→ 跳过，画缓存帧
3. 低分辨率 FNV-1a 哈希（8×8 降采样）
4. 哈希与上一帧相同？→ 跳过，画缓存帧
5. SKSurface.Snapshot(cropRect) ← GPU 操作，零 CPU roundtrip
6. SKImageFilter.CreateBlur(sigma) → 高斯模糊
7. SKPaint { Color = tintColor, BlendMode = SrcOver } → 叠加主题色
8. 绘制到 DrawingContext
9. 缓存本次帧数据和哈希值
```

**实时更新：**

- `FrostedGlassCard` 内部持有一个 `DispatcherTimer`（DispatcherPriority.Render）
- Tick 间隔 66ms → 调用 `InvalidateVisual()`
- `Render()` 内通过时间闸（`now - lastCaptureMs < 66`）控制实际截取频率
- 哈希进一步过滤：内容没变就不截取 + 不模糊
- Modal 关闭 → `OnDetachedFromVisualTree` → Timer Stop，自动退出渲染循环

### Modal ControlTheme 覆写

在 Polymerium 中覆写 `husk:Modal` 的 ControlTheme，将原有实心背景替换为 `FrostedGlassCard`：

- Modal 原有的 `FlyoutBackgroundBrush` 改为 `Transparent`
- 模板中外层 Border 保留 `CornerRadius` + `ClipToBounds="True"` + `BoxShadow`
- FrostedGlassCard 填充整个 Border 区域，模糊后的边缘由父 Border 剪切为圆角

Tint 颜色随主题变化：
- Light：`#B3FFFFFF`（70% 白）
- Dark：`#B31E1E1E`（70% 黑）

### 文件清单

| 文件 | 行数 | 职责 |
|---|---|---|
| `Controls/FrostedGlassCard.cs` | ~80 | ContentControl + DispatcherTimer + 哈希缓存管理 |
| `Controls/FrostedGlassCardRenderer.cs` | ~120 | ICustomDrawOperation，Snapshot → Blur → Tint |
| `Controls/FrostedGlassCard.axaml` | ~30 | 极简 ControlTheme + Modal 覆写 |
| 修改 `Themes/Controls.axaml` | +1 | 引入新文件 |

## 依赖与约束

- 需要 `Avalonia.Skia` 渲染后端（SkiaSharp 4.148.0 已在依赖中）
- 不需要 `AllowUnsafeBlocks`（不涉及像素指针操作）
- 不需要反射（不访问未公开 API）
- `FrostedGlassCardRenderer` 通过 `ISkiaSharpApiLeaseFeature` 获取底层 Skia canvas
- CornerRadius 由上级 Border + ClipToBounds 承担，FrostedGlassCard 画满矩形

## 与上游库的边界

- 不修改 Huskui.Avalonia（NuGet 包）
- 不修改 OverlayHost 模板（覆写 Modal ControlTheme 即可）
- 不对 OverlayHost 的 SmokeMask 做任何改动
