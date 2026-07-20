using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
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
///     毛玻璃背景控件：截取后方可视树内容 → GPU 高斯模糊 → 主题色叠加。内容静止时不重绘，变化时上限 15fps。
/// </summary>
public class BlurBackdrop : ContentControl
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(BlurRadius), 16.0);

    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<BlurBackdrop, Color>(nameof(TintColor), Colors.Transparent);

    public static readonly StyledProperty<double> TintOpacityProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(TintOpacity), 1.0);

    /// <summary>
    ///     模糊链路任意一环失败（无 GPU 上下文、snapshot 不可租、blur 抛错、capture 抛错）时绘制的纯色底。
    ///     失败必须可观察——绝不静默回退到 CPU 模糊。
    /// </summary>
    public static readonly StyledProperty<IBrush?> FallbackBrushProperty =
        AvaloniaProperty.Register<BlurBackdrop, IBrush?>(nameof(FallbackBrush));

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

    /// <summary>
    ///     按 <see cref="StyledElement.Name" /> 匹配、全局排除出捕获的视觉元素集合。Huskui 的 SmokeMask 等
    ///     NuGet 宿主控件无法挂 <see cref="ExcludeFromCaptureProperty" />，只能在此按名登记；待 BlurBackdrop
    ///     并入上游 Huskui 后改用附加属性，此集合可清空。
    /// </summary>
    public static HashSet<string> ExcludedRoots { get; } = new(StringComparer.Ordinal);

    // 重绘频率上限（15fps）。SceneInvalidated 频率远高于此，这里只做上限节流。
    private static readonly long MIN_INTERVAL_TICKS = TimeSpan.FromMilliseconds(66).Ticks;

    private static PropertyInfo? _topLevelRendererProperty;
    private static PropertyInfo? _dirtyRectProperty;

    private RenderTargetBitmap? _scratch;
    // NOTE: _current 由 Capture（UI 线程，经 RAF）写、Render/drawOp（渲染线程）读——不同线程。BackdropSnapshot
    //       用引用计数租赁（TryAddLease/ReleaseLease）保证渲染线程持有时 native SKImage 不会被释放，跨线程安全。
    private BackdropSnapshot? _current;
    // 是否已成功捕获过至少一帧。未就绪（首帧前）时 Render 透明跳过；就绪后 snapshot 缺失才视为失败走 Fallback。
    private volatile bool _everCaptured;
    private ulong _lastHash;
    private long _lastCaptureTicksUtc;
    private bool _captureQueued;
    private bool _detached;
    private bool _polling;
    private TopLevel? _topLevel;

    private object? _renderer;
    private EventInfo? _sceneInvalidatedEvent;
    private Delegate? _sceneInvalidatedHandler;

    static BlurBackdrop() => AffectsRender<BlurBackdrop>(
        BlurRadiusProperty,
        TintColorProperty,
        TintOpacityProperty,
        CornerRadiusProperty,
        FallbackBrushProperty);

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

    public IBrush? FallbackBrush
    {
        get => GetValue(FallbackBrushProperty);
        set => SetValue(FallbackBrushProperty, value);
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
        var old = Interlocked.Exchange(ref _current, null);
        old?.ReleaseLease();
        _everCaptured = false;
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        // 未就绪（从未成功捕获）时透明跳过；这不是失败，是正常的启动首帧。
        if (!_everCaptured || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var snapshot = Volatile.Read(ref _current);
        using (context.PushClip(new RoundedRect(new Rect(Bounds.Size), CornerRadius)))
        {
            context.Custom(new BackdropDrawOperation(
                new(Bounds.Size),
                snapshot,
                BlurRadius,
                TintColor,
                (float)TintOpacity,
                ResolveFallback()));
        }
    }

    private SKColor ResolveFallback()
    {
        if (FallbackBrush is ISolidColorBrush solid)
            return new(solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A);
        // 未显式设置 FallbackBrush 时退化为 tint 全不透明——保证失败可见，而非透明。
        var tint = TintColor;
        return new(tint.R, tint.G, tint.B, tint.A);
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

            var evt = renderer?.GetType().GetEvent(
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
        if (_everCaptured
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
        _dirtyRectProperty ??= e.GetType().GetProperty(
            "DirtyRect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (_dirtyRectProperty?.PropertyType != typeof(Rect))
            return false;
        if (_dirtyRectProperty.GetValue(e) is not Rect value)
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
                // 捕获链路失败：丢弃旧 snapshot，让 Render 走 Fallback——失败必须可观察。
                var old = Interlocked.Exchange(ref _current, null);
                old?.ReleaseLease();
                InvalidateVisual();
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

        // 先读 8 条采样行做 hash，匹配则跳过整图拷贝。计时器无论是否匹配都推进，保证 ConsiderCapture 的
        // 15fps 节流覆盖整个捕获（含采样），否则静态内容会让采样每帧空转。
        var hash = SampleHash(_scratch, cw, ch, rowBytes);
        _lastCaptureTicksUtc = DateTime.UtcNow.Ticks;
        if (_everCaptured && hash == _lastHash)
            return;
        _lastHash = hash;

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

            var controlOffsetPx = new Vector(
                                       (controlInTop.X - captureRect.X) * scaling,
                                       (controlInTop.Y - captureRect.Y) * scaling);
            var controlSizePx = new Vector(controlInTop.Width * scaling, controlInTop.Height * scaling);

            // NOTE: color type 必须用 PlatformColorType——RenderTargetBitmap 在 macOS/Linux 输出 Rgba8888、
            //       Windows 输出 Bgra8888，硬编码任一都会在另一个平台 R/B 反色。FromPixelCopy 会拷贝一
            //       份自有像素，此后 pixels 可安全归还池。
            SKImageInfo info = new(cw, ch, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            var image = SKImage.FromPixelCopy(info, pixels, rowBytes);

            var snapshot = new BackdropSnapshot(image, controlOffsetPx, controlSizePx, scaling);
            var old = Interlocked.Exchange(ref _current, snapshot);
            old?.ReleaseLease();
            _everCaptured = true;
            InvalidateVisual();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixels);
        }
    }

    private static ulong SampleHash(RenderTargetBitmap bmp, int cw, int ch, int rowBytes)
    {
        const ulong fnvOffset = 14695981039346656037UL;
        const ulong fnvPrime = 1099511628211UL;
        const int samples = 8;
        var hash = fnvOffset;
        var maxX = Math.Max(1, cw) - 1;
        var maxY = Math.Max(1, ch) - 1;

        var strip = ArrayPool<byte>.Shared.Rent(rowBytes);
        var handle = GCHandle.Alloc(strip, GCHandleType.Pinned);
        try
        {
            for (var sy = 0; sy < samples; sy++)
            {
                var y = (int)((long)sy * maxY / (samples - 1));
                bmp.CopyPixels(new(0, y, cw, 1), handle.AddrOfPinnedObject(), rowBytes, rowBytes);
                for (var sx = 0; sx < samples; sx++)
                {
                    var x = (int)((long)sx * maxX / (samples - 1));
                    var off = x * 4;
                    for (var i = 0; i < 4; i++)
                        hash = (hash ^ strip[off + i]) * fnvPrime;
                }
            }
        }
        finally
        {
            handle.Free();
            ArrayPool<byte>.Shared.Return(strip);
        }

        hash = (hash ^ (ulong)cw) * fnvPrime;
        hash = (hash ^ (ulong)ch) * fnvPrime;
        return hash;
    }

    private static object? GetRenderer(TopLevel topLevel)
    {
        _topLevelRendererProperty ??= typeof(TopLevel).GetProperty(
            "Renderer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return _topLevelRendererProperty?.GetValue(topLevel);
    }

    /// <summary>
    ///     捕获到的原始（未模糊）位图 + 渲染线程安全租赁。Capture 持有初始计数 1，渲染线程每次绘制前后
    ///     TryAddLease/ReleaseLease；计数归零时释放 native SKImage。
    /// </summary>
    private sealed class BackdropSnapshot
    {
        private int _refs; // 1 = 由 capture 槽位持有；渲染线程每次租赁 +1

        public SKImage Image { get; }
        public Vector ControlOffsetPx { get; }
        public Vector ControlSizePx { get; }
        public double Scaling { get; }

        public BackdropSnapshot(SKImage image, Vector controlOffsetPx, Vector controlSizePx, double scaling)
        {
            Image = image;
            ControlOffsetPx = controlOffsetPx;
            ControlSizePx = controlSizePx;
            Scaling = scaling;
            _refs = 1;
        }

        // 渲染线程尝试租赁。已释放或正在释放时返回 false——调用方据此走 Fallback，绝不读到已释放的 SKImage。
        public bool TryAddLease()
        {
            while (true)
            {
                var r = Volatile.Read(ref _refs);
                if (r <= 0)
                    return false;
                if (Interlocked.CompareExchange(ref _refs, r + 1, r) == r)
                    return true;
            }
        }

        public void ReleaseLease()
        {
            if (Interlocked.Decrement(ref _refs) == 0)
                Image.Dispose();
        }
    }

    private sealed class BackdropDrawOperation(
        Rect bounds,
        BackdropSnapshot? snapshot,
        double blurRadius,
        Color tint,
        float tintOpacity,
        SKColor fallback) : ICustomDrawOperation
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

            // NOTE: 无 GPU 上下文（软件渲染 / GPU 丢失）时不静默回退 CPU 模糊——直接画 Fallback，让降级可观察。
            if (lease.GrContext is null)
            {
                FillFallback(canvas);
                return;
            }

            if (snapshot is null || !snapshot.TryAddLease())
            {
                FillFallback(canvas);
                return;
            }

            try
            {
                SKRect src = new(
                    (float)snapshot.ControlOffsetPx.X,
                    (float)snapshot.ControlOffsetPx.Y,
                    (float)(snapshot.ControlOffsetPx.X + snapshot.ControlSizePx.X),
                    (float)(snapshot.ControlOffsetPx.Y + snapshot.ControlSizePx.Y));
                SKRect dst = new(0f, 0f, (float)bounds.Width, (float)bounds.Height);

                // 把捕获的原始位图（含 blurMargin 余量）直接以 ImageFilter 方式画到 lease 的画布上，模糊由
                // Skia 在 GPU 上执行；全程不创建自定义 GPU surface，规避 GRContext 生命周期问题。
                var sigma = (float)(blurRadius * snapshot.Scaling);
                using var blur = SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Clamp, null);
                using var paint = new SKPaint { ImageFilter = blur };
                canvas.DrawImage(
                    snapshot.Image,
                    src,
                    dst,
                    new SKSamplingOptions(SKFilterMode.Linear),
                    paint);

                var a = (byte)Math.Clamp(tint.A * tintOpacity, 0, 255);
                using var tintPaint = new SKPaint { Color = new(tint.R, tint.G, tint.B, a) };
                canvas.DrawRect(dst, tintPaint);
            }
            catch
            {
                FillFallback(canvas);
            }
            finally
            {
                snapshot.ReleaseLease();
            }

            void FillFallback(SKCanvas c)
            {
                using var p = new SKPaint();
                p.Color = fallback;
                c.DrawRect(new(0f, 0f, (float)bounds.Width, (float)bounds.Height), p);
            }
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
            if (visual is StyledElement { Name: { } name } && BlurBackdrop.ExcludedRoots.Contains(name))
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

        // NOTE: 不复现后方元素的圆角裁剪——曾用 PushClip(fromClipBounds) 出现裁切偏移且无法修复，现已移除。
        //       让图形元素自行裁剪即可，模糊后圆角差异肉眼不可分辨。
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
