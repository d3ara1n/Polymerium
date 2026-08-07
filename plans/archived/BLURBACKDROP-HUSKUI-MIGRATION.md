> 制定日期：2026-07-09
> 定位：把 Polymerium 自研的 BlurBackdrop 毛玻璃控件迁回 Huskui.Avalonia 主题库
> 当前状态：❌ 不予回迁（已废弃）

## 结论

**BlurBackdrop 毛玻璃效果不达标，决定不回迁 Huskui，此计划作废。** 实际验证下来视觉效果差（糊、发灰、噪点明显），不足以作为主题库的通用能力对外提供。Huskui 侧不引入 BlurBackdrop、不新增 Frosted 变体。Polymerium 侧现有的 `BlurBackdrop` / Frosted Modal 保持原样不动，用着就用着。

留此文件作否决理由备案：后续若再有人提议把毛玻璃迁回 Huskui，先读这里的结论，避免重复讨论。

---

# BlurBackdrop 迁移到 Huskui — Checklist

## 前提

Polymerium 内的 `BlurBackdrop`（`Controls/BlurBackdrop.cs` + `Controls/BlurBackdrop.axaml`）、`Modal` 的 Frosted 变体（`Controls/Modal.axaml`）以及各 Modal 的 `Classes="Frosted"` 套用都已稳定运行一段时间——视觉效果、性能（静止不重绘 / ≤15fps）、关闭不闪退都验证通过后再迁移。

## 代码搬运

- [ ] 新建 `src/Huskui.Avalonia/Controls/BlurBackdrop.cs`，从 Polymerium 搬过来，`namespace` 由 `Polymerium.Avalonia.Controls` 改为 `Huskui.Avalonia.Controls`
- [ ] 新建 `src/Huskui.Avalonia/Controls/BlurBackdrop.axaml`（ControlTheme）
- [ ] 把 Frosted 变体并入 Huskui 的 `Modal` 主题（`src/Huskui.Avalonia/Controls/Modal.axaml` 加 `^.Frosted` Style），而非 Polymerium 侧覆写
- [ ] Huskui 的控件主题聚合处加 `ResourceInclude`（BlurBackdrop.axaml）

## 主题资源

- [ ] `BlurBackdropTintColor` 从 `BlurBackdrop.axaml` 的 `ThemeDictionaries` 拆到 Huskui 的 Colors 体系（加进 `Themes/Colors.Overlay.axaml` 或新建 `Colors.Backdrop.axaml`），对齐 Huskui 现有 `Overlay1Color` 那套 Color 键惯例

## 清掉 SmokeMask 硬编码

- [ ] `src/Huskui.Avalonia/Controls/OverlayHost.axaml` 给 SmokeMask 挂 `BlurBackdrop.ExcludeFromCapture="True"`（同项目内，可直接用 `local:`）
- [ ] 删掉 `BackdropVisualRenderer.ShouldExclude` 里的 `Name == "PART_SmokeMask"` 分支，只保留 `ExcludeFromCapture` 判断

## 处理已知 HACK

- [x] 重渲整个 topLevel 再裁出控件区域的 HACK 已解决。根因：`BackdropVisualRenderer` 入口的 `PushClip(new Rect(clipRect.Size))` 在 software rendering + 前置负平移 `PushTransform(-clipRect.X)` 的组合下会把右侧裁掉；全窗模式因 `clipRect.X=0`、负平移为零而从未暴露。修复 = 去掉 `PushClip`，靠 `RenderTargetBitmap` 画布边界做天然 clip，captureRect 外的子树仍由递归里的 `visualBounds.Intersects(clipRect)` 跳过。Capture 同时改成只渲染 captureRect 区域（不再整窗软渲）。顺带修了 CopyPixels 字节序反色（`SKColorType.Bgra8888` → `SKImageInfo.PlatformColorType`）。迁移时直接搬这套已验证的实现。

## Polymerium 侧清理

- [ ] 删 `src/Polymerium.Avalonia/Controls/BlurBackdrop.cs`、`BlurBackdrop.axaml`、`Modal.axaml`
- [ ] 删 `src/Polymerium.Avalonia/Themes/Controls.axaml` 里的 BlurBackdrop + Modal 两个 `ResourceInclude`
- [ ] 各 Modal 的 `Classes="Frosted"` 保留不动——Frosted 变体移入 Huskui 后由 Huskui 的 Modal 主题提供，Polymerium 侧无需改
- [ ] 把 `Huskui.Avalonia` 的引用升级到含 BlurBackdrop + Frosted 变体的版本

## Huskui 侧集成 / 验证

- [ ] 确认 Huskui 的 `XmlnsDefinition` 已把 `Huskui.Avalonia.Controls` 映射到 husk: scheme（应该已有，OverlayHost 等都在该命名空间）
- [ ] Huskui Gallery 加 BlurBackdrop + Frosted Modal demo（参考 `Views/ModalsPage.axaml` 等）
- [ ] API 审视：`BlurRadius` / `TintColor` / `TintOpacity` / `CornerRadius` / `ExcludeFromCapture` 命名是否符合 Huskui 习惯；`ExcludeFromCapture` 是否公开（第三方也可能要排除元素）

## 迁移后回归

- [ ] Polymerium 切到新 Huskui 版本后，各 Frosted Modal 毛玻璃效果一致（圆角、tint、不闪退、静止不刷）
- [ ] Light / Dark 主题切换 tint 跟随
- [ ] SmokeMask 排除仍有效（tint 不偏黑）
