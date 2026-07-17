using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     毛玻璃背景控件：截取后方可视树 → 软件捕获 → GPU 高斯模糊 → 主题色叠加。捕获在 UI 线程做（软件
///     光栅化），模糊在 render 线程的 GPU surface 上执行；脏区未触及后方时跳过捕获，变化时上限 60fps。
/// </summary>
// TODO: 开发机（弱 CPU）上效果马马虎虎——捕获的软件光栅化吃 UI 线程。GPU 模糊 + DirtyRect 跳过的实际
//       收益需在游戏机（目标机器，CPU/GPU 强）上验证，只能等正式版本发布后由用户在那边测。
public class BlurBackdrop : ContentControl
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(BlurRadius), 16.0);

    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<BlurBackdrop, Color>(nameof(TintColor), Colors.Transparent);

    public static readonly StyledProperty<double> TintOpacityProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(TintOpacity), 1.0);

    /// <summary>
    ///     挂在任何 Visual 上，使其（及子树）不参与后方内容捕获。用于排除会污染 tint 的半透明遮罩等。
    /// </summary>
    public static readonly AttachedProperty<bool> ExcludeFromCaptureProperty =
        AvaloniaProperty.RegisterAttached<BlurBackdrop, Visual, bool>(
            "ExcludeFromCapture",
            defaultValue: false);

    /// <summary>
    ///     挂在内置 BlurBackdrop 背景层的 overlay（Modal/Dialog/Sidebar/Toast）上，关闭其默认毛玻璃并恢复不透明底色。
    ///     异型控件设为 False 后可在自身内容里手动放置 BlurBackdrop 自定义模糊区域。
    /// </summary>
    public static readonly AttachedProperty<bool> UseBlurProperty =
        AvaloniaProperty.RegisterAttached<BlurBackdrop, Control, bool>(
            "UseBlur",
            defaultValue: true);

    // 重绘频率上限（60fps）。模糊在 DrawOperation 的 GPU surface 上执行，捕获是唯一 CPU 开销。
    private static readonly long MIN_INTERVAL_TICKS = TimeSpan.FromMilliseconds(16).Ticks;

    private static PropertyInfo? _topLevelRendererProperty;
    private static PropertyInfo? s_dirtyRectProperty;

    private RenderTargetBitmap? _scratch;
    // NOTE: _backdrop 被 Capture 写、Render 读。当前 RAF/SceneInvalidated 与控件 Render 实测同线程，加 volatile
    //       保证可见性；彻底的跨线程模型待阶段二随 GPU 重构一并确认。
    private volatile SKImage? _backdrop;
    private Vector _controlOffsetPx;
    private Vector _controlSizePx;
    private double _scaling;
    private ulong _lastHash;
    private long _lastCaptureTicksUtc;
    private bool _captureQueued;
    private bool _detached;
    private bool _polling;
    private TopLevel? _topLevel;

    private object? _renderer;
    private EventInfo? _sceneInvalidatedEvent;
    private Delegate? _sceneInvalidatedHandler;

    static BlurBackdrop() => AffectsRender<BlurBackdrop>(BlurRadiusProperty, TintColorProperty, TintOpacityProperty, CornerRadiusProperty);

    public double BlurRadius
    {
        get => GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    public Color TintColor
    {
        get => GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public double TintOpacity
    {
        get => GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    public static bool GetExcludeFromCapture(Visual element) => element.GetValue(ExcludeFromCaptureProperty);

    public static void SetExcludeFromCapture(Visual element, bool value) =>
        element.SetValue(ExcludeFromCaptureProperty, value);

    public static bool GetUseBlur(Control element) => element.GetValue(UseBlurProperty);

    public static void SetUseBlur(Control element, bool value) => element.SetValue(UseBlurProperty, value);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _detached = false;
        EnsureRendererSubscription();
        QueueCapture();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _detached = true;
        DetachRendererSubscription();
        _scratch?.Dispose();
        _scratch = null;
        // NOTE: 不主动 dispose _backdrop——detach 与最后一次 Render（渲染线程）存在交错窗口，主动释放可能让
        //       Render 读到已 dispose 的 SKImage。置 null 让 Render 安全 short-circuit，native 资源交 GC 兜底。
        _backdrop = null;
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        var backdrop = _backdrop;
        if (backdrop is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        // 只裁剪毛玻璃背景层；内容（模板的 ContentPresenter）由框架独立渲染，不受此 clip 影响。
        var corner = CornerRadius;
        var rounded = corner != default;
        var clip = rounded
                       ? context.PushClip(new RoundedRect(new(Bounds.Size), corner))
                       : default;
        try
        {
            context.Custom(new BackdropDrawOperation(
                new(Bounds.Size),
                backdrop,
                _controlOffsetPx,
                _controlSizePx,
                BlurRadius,
                _scaling,
                TintColor,
                (float)TintOpacity));
        }
        finally
        {
            if (rounded)
                clip.Dispose();
        }
    }

    // NOTE: SceneInvalidated 是 Avalonia 渲染器的 internal 信号（IRenderer 整个接口标 [PrivateApi]），无公开
    //       等价物——Avalonia 自身也用它更新 ToolTip。反射订阅拿"内容真变化"的精确触发；若跨大版本改名导致
    //       反射失败，自动回退 RequestAnimationFrame 轮询：精度和功耗变差，但控件仍工作，不会变成死灰块。
    //       升级 Avalonia 大版本时重点回归这一处。
    private void EnsureRendererSubscription()
    {
        if (_sceneInvalidatedHandler is not null || _polling)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;
        _topLevel = topLevel;

        if (TrySubscribeSceneInvalidated(topLevel))
            return;

        _polling = true;
        topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private bool TrySubscribeSceneInvalidated(TopLevel topLevel)
    {
        try
        {
            var renderer = GetRenderer(topLevel);
            if (renderer is null)
                return false;

            var evt = renderer.GetType().GetEvent(
                                                  "SceneInvalidated",
                                                  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (evt is null)
                return false;

            EventHandler<SceneInvalidatedEventArgs> handler = OnSceneInvalidated;
            evt.AddEventHandler(renderer, handler);
            _renderer = renderer;
            _sceneInvalidatedEvent = evt;
            _sceneInvalidatedHandler = handler;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void OnAnimationFrame(TimeSpan _)
    {
        ConsiderCapture();
        if (_polling && !_detached && _topLevel is not null)
            _topLevel.RequestAnimationFrame(OnAnimationFrame);
    }

    private void DetachRendererSubscription()
    {
        _polling = false;
        if (_renderer is not null && _sceneInvalidatedEvent is not null && _sceneInvalidatedHandler is not null)
            _sceneInvalidatedEvent.RemoveEventHandler(_renderer, _sceneInvalidatedHandler);

        _renderer = null;
        _sceneInvalidatedEvent = null;
        _sceneInvalidatedHandler = null;
        _topLevel = null;
    }

    private void OnSceneInvalidated(object? sender, SceneInvalidatedEventArgs e)
    {
        // DirtyRect 优化：这一帧重绘的区域若完全落在自身 bounds 内（典型是 Capture 末尾 InvalidateVisual
        // 触发），说明只是自身重绘、后方内容没变 → 跳过捕获，省掉最贵的软件光栅化。脏区延伸到外部才捕获。
        if (_backdrop is not null
            && TryGetDirtyRect(e, out var dirtyRect)
            && IsSelfOnlyDirtyRect(dirtyRect))
            return;

        ConsiderCapture();
    }

    private bool IsSelfOnlyDirtyRect(Rect dirtyRect)
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel
            || this.TransformToVisual(topLevel) is not { } toTopLevel)
            return false;

        var selfInTop = new Rect(Bounds.Size).TransformToAABB(toTopLevel);
        return selfInTop.Contains(dirtyRect);
    }

    private static bool TryGetDirtyRect(SceneInvalidatedEventArgs e, out Rect dirtyRect)
    {
        dirtyRect = default;
        s_dirtyRectProperty ??= e.GetType().GetProperty(
            "DirtyRect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (s_dirtyRectProperty?.PropertyType != typeof(Rect))
            return false;
        if (s_dirtyRectProperty.GetValue(e) is not Rect value)
            return false;

        dirtyRect = value;
        return true;
    }

    private void ConsiderCapture()
    {
        if (_detached)
            return;

        if (DateTime.UtcNow.Ticks - _lastCaptureTicksUtc < MIN_INTERVAL_TICKS)
            return;

        QueueCapture();
    }

    private void QueueCapture()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || _captureQueued)
            return;

        _captureQueued = true;
        topLevel.RequestAnimationFrame(_ =>
        {
            _captureQueued = false;
            if (_detached)
                return;
            try
            {
                Capture();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BlurBackdrop capture failed: {ex.Message}");
            }
        });
    }

    private void Capture()
    {
        if (_detached)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        if (this.TransformToVisual(topLevel) is not { } toTopLevel)
            return;

        var controlInTop = new Rect(Bounds.Size).TransformToAABB(toTopLevel);
        var scaling = topLevel.RenderScaling;
        var blurMargin = Math.Max(8.0, BlurRadius * 3.0);
        var captureRect = controlInTop.Inflate(blurMargin).Intersect(new(topLevel.ClientSize));
        if (captureRect.Width <= 0 || captureRect.Height <= 0)
            return;

        // 只渲染控件区域（含 blurMargin），bitmap 尺寸 = captureRect 像素尺寸，省去整窗软件渲染。
        PixelSize pixel = new(
            (int)Math.Ceiling(captureRect.Width * scaling),
            (int)Math.Ceiling(captureRect.Height * scaling));
        if (_scratch is null || _scratch.PixelSize != pixel)
        {
            _scratch?.Dispose();
            _scratch = new(pixel, new(96.0 * scaling, 96.0 * scaling));
        }

        using (var ctx = _scratch.CreateDrawingContext())
            BackdropVisualRenderer.Render(ctx, topLevel, this, captureRect);

        const int bpp = 4;
        var cw = pixel.Width;
        var ch = pixel.Height;
        var rowBytes = cw * bpp;
        var length = rowBytes * ch;
        // 复用 ArrayPool，避免每帧分配捕获区像素。Rent 返回的 buffer 可能长于 length，后续访问全部用
        // cw/ch/rowBytes 限定，不触碰多余尾部。
        var pixels = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                _scratch.CopyPixels(new(0, 0, cw, ch), handle.AddrOfPinnedObject(), length, rowBytes);
            }
            finally
            {
                handle.Free();
            }

            var hash = HashPixels(pixels, cw, ch, rowBytes);
            _lastCaptureTicksUtc = DateTime.UtcNow.Ticks;

            if (_backdrop is not null && hash == _lastHash)
                return;
            _lastHash = hash;

            _controlOffsetPx = new(
                                   (controlInTop.X - captureRect.X) * scaling,
                                   (controlInTop.Y - captureRect.Y) * scaling);
            _controlSizePx = new(controlInTop.Width * scaling, controlInTop.Height * scaling);
            _scaling = scaling;

            // NOTE: 不在此处模糊——模糊挪到 DrawOperation 的 GPU surface 上执行（CPU 模糊吃 UI 线程）。
            //       color type 必须用 PlatformColorType——跨平台 R/B 一致性。
            SKImageInfo info = new(cw, ch, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            var captured = SKImage.FromPixelCopy(info, pixels, rowBytes);

            _backdrop?.Dispose();
            _backdrop = captured;
            InvalidateVisual();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixels);
        }
    }

    private static ulong HashPixels(byte[] pixels, int width, int height, int rowBytes)
    {
        const ulong fnvOffset = 14695981039346656037UL;
        const ulong fnvPrime = 1099511628211UL;
        var hash = fnvOffset;
        const int samples = 8;
        var maxX = Math.Max(1, width) - 1;
        var maxY = Math.Max(1, height) - 1;

        for (var sy = 0; sy < samples; sy++)
        {
            var y = (int)((long)sy * maxY / (samples - 1));
            var rowOff = y * rowBytes;
            for (var sx = 0; sx < samples; sx++)
            {
                var x = (int)((long)sx * maxX / (samples - 1));
                var off = rowOff + x * 4;
                for (var i = 0; i < 4; i++)
                    hash = (hash ^ pixels[off + i]) * fnvPrime;
            }
        }

        hash = (hash ^ (ulong)width) * fnvPrime;
        hash = (hash ^ (ulong)height) * fnvPrime;
        return hash;
    }

    private static object? GetRenderer(TopLevel topLevel)
    {
        _topLevelRendererProperty ??= typeof(TopLevel).GetProperty(
            "Renderer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return _topLevelRendererProperty?.GetValue(topLevel);
    }

    private sealed class BackdropDrawOperation(
        Rect bounds,
        SKImage backdrop,
        Vector controlOffsetPx,
        Vector controlSizePx,
        double blurRadius,
        double scaling,
        Color tint,
        float tintOpacity) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
                return;

            using var lease = leaseFeature.Lease();
            var canvas = lease.SkCanvas;

            // GPU 模糊整张捕获图（含 margin，边缘不衰减）；无 GPU 上下文时回退 CPU surface。
            var sigmaPx = (float)(blurRadius * scaling);
            SKImageInfo info = new(backdrop.Width, backdrop.Height, backdrop.ColorType, backdrop.AlphaType);
            using var surface = CreateBlurSurface(info, lease.GrContext);
            var off = surface.Canvas;
            off.Clear(SKColors.Transparent);
            using (var blur = SKImageFilter.CreateBlur(sigmaPx, sigmaPx, SKShaderTileMode.Clamp, null))
            using (var paint = new SKPaint { ImageFilter = blur, BlendMode = SKBlendMode.Src })
            {
                off.DrawImage(backdrop, 0, 0, new(SKFilterMode.Linear), paint);
            }
            off.Flush();
            using var blurred = surface.Snapshot();

            SKRect src = new(
                (float)controlOffsetPx.X,
                (float)controlOffsetPx.Y,
                (float)(controlOffsetPx.X + controlSizePx.X),
                (float)(controlOffsetPx.Y + controlSizePx.Y));
            SKRect dst = new(0f, 0f, (float)bounds.Width, (float)bounds.Height);
            if (src.Width <= 0 || src.Height <= 0)
                return;

            canvas.DrawImage(blurred, src, dst, new SKSamplingOptions(SKFilterMode.Linear));

            var a = (byte)Math.Clamp(tint.A * tintOpacity, 0, 255);
            using SKPaint tintPaint = new();
            tintPaint.Color = new(tint.R, tint.G, tint.B, a);
            canvas.DrawRect(dst, tintPaint);
        }

        private static SKSurface CreateBlurSurface(SKImageInfo info, GRContext? grContext)
        {
            if (grContext is not null)
            {
                var gpu = SKSurface.Create(grContext, false, info);
                if (gpu is not null)
                    return gpu;
            }

            return SKSurface.Create(info);
        }
    }

    private static class BackdropVisualRenderer
    {
        // NOTE: 只捕获绘制顺序在 target 之前（其"背后"）的内容；target 自身及之后（之上）的兄弟/子树
        //       一律跳过——否则 overlay 自己的前景内容会进捕获、随滚动改变 hash，导致每帧重模糊。
        public static void Render(DrawingContext context, Visual root, Visual target, Rect clipRect)
        {
            if (clipRect.Width <= 0 || clipRect.Height <= 0)
                return;

            // NOTE: 不加 PushClip——RenderTargetBitmap 自身画布边界已是天然 clip，额外的 PushClip 在
            //       software rendering + 负平移组合下会把右侧裁掉（实测）。captureRect 外的子树靠下面
            //       visualBounds.Intersects(clipRect) 跳过，超出画布的绘制由 RenderTargetBitmap 丢弃。
            using (context.PushTransform(Matrix.CreateTranslation(-clipRect.X, -clipRect.Y)))
            {
                Render(context, root, target, new(root.Bounds.Size), Matrix.Identity, clipRect);
            }
        }

        private static bool ShouldExclude(Visual visual)
        {
            // NOTE: 内置排除 Huskui OverlayHost 的 SmokeMask 半透明遮罩——其黑色叠在后方内容上，经模糊
            //       + tint 后整体偏黑。当前靠 Name 匹配（Huskui 模板内部命名）；移入 Huskui 后改由
            //       OverlayHost 给 SmokeMask 挂 ExcludeFromCapture 标记，去掉这条硬编码。
            if (visual is StyledElement { Name: "PART_SmokeMask" })
                return true;

            return visual.GetValue(ExcludeFromCaptureProperty);
        }

        // 返回 true 表示已命中 target：调用方据此停止遍历后续兄弟（绘制顺序在 target 之上，不进捕获）。
        private static bool Render(
            DrawingContext context,
            Visual visual,
            Visual target,
            Rect bounds,
            Matrix parentTransform,
            Rect clipRect)
        {
            if (visual == target)
                return true;

            if (ShouldExclude(visual))
                return false;

            if (!visual.IsVisible || visual.Opacity <= 0)
                return false;

            Rect rect = new(bounds.Size);
            Matrix transform;
            if (visual.RenderTransform?.Value is { } rt)
            {
                var origin = visual.RenderTransformOrigin.ToPixels(visual.Bounds.Size);
                var offset = Matrix.CreateTranslation(origin);
                transform = -offset * rt * offset * Matrix.CreateTranslation(bounds.Position);
            }
            else
            {
                transform = Matrix.CreateTranslation(bounds.Position);
            }

            using (context.PushTransform(transform))
            using (context.PushOpacity(visual.Opacity))
            using (PushClipToBounds(context, visual, rect))
            using (visual.Clip is { } clip ? context.PushGeometryClip(clip) : default(DrawingContext.PushedState?))
            using (visual.OpacityMask is { } opacityMask
                ? context.PushOpacityMask(opacityMask, rect)
                : default(DrawingContext.PushedState?))
            {
                var totalTransform = transform * parentTransform;
                var visualBounds = rect.TransformToAABB(totalTransform);

                if (visualBounds.Intersects(clipRect))
                    visual.Render(context);

                var children = GetOrderedChildren(visual);

                var childClip = clipRect;
                var childParent = totalTransform;
                if (visual.ClipToBounds)
                {
                    childParent = Matrix.Identity;
                    childClip = rect;
                }

                foreach (var child in children)
                {
                    if (Render(context, child, target, child.Bounds, childParent, childClip))
                        return true;
                }
                return false;
            }
        }

        // NOTE: 不复现后方元素的圆角裁剪——模糊后圆角与直角肉眼不可分辨，省掉一处 internal 反射
        //       (Visual.ClipToBoundsRadius 经 IVisualWithRoundRectClip 暴露，接口非公开)。
        private static DrawingContext.PushedState? PushClipToBounds(DrawingContext context, Visual visual, Rect rect)
        {
            return visual.ClipToBounds ? context.PushClip(rect) : default;
        }

        private static IReadOnlyList<Visual> GetOrderedChildren(Visual visual)
        {
            List<Visual>? list = null;
            int? firstZIndex = null;
            var hasNonUniformZIndex = false;

            foreach (var child in visual.GetVisualChildren())
            {
                list ??= [];
                list.Add(child);
                if (firstZIndex is null)
                    firstZIndex = child.ZIndex;
                else if (child.ZIndex != firstZIndex.Value)
                    hasNonUniformZIndex = true;
            }

            if (list is null || list.Count == 0)
                return [];

            return hasNonUniformZIndex ? list.OrderBy(x => x.ZIndex).ToArray() : list;
        }
    }
}
