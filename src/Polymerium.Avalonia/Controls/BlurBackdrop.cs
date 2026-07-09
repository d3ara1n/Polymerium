using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using SkiaSharp;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     毛玻璃背景控件：截取后方可视树 → 高斯模糊 → 主题色叠加。内容静止时不重绘，变化时上限 15fps。
/// </summary>
public class BlurBackdrop : ContentControl
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(BlurRadius), 16.0);

    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<BlurBackdrop, Color>(nameof(TintColor), Colors.Transparent);

    public static readonly StyledProperty<double> TintOpacityProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(TintOpacity), 1.0);

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<BlurBackdrop, CornerRadius>(nameof(CornerRadius));

    /// <summary>
    ///     挂在任何 Visual 上，使其（及子树）不参与后方内容捕获。用于排除会污染 tint 的半透明遮罩等。
    /// </summary>
    public static readonly AttachedProperty<bool> ExcludeFromCaptureProperty =
        AvaloniaProperty.RegisterAttached<BlurBackdrop, Visual, bool>(
            "ExcludeFromCapture",
            defaultValue: false);

    // 重绘频率上限（15fps）。SceneInvalidated 频率远高于此，这里只做上限节流。
    private static readonly long s_minIntervalTicks = TimeSpan.FromMilliseconds(66).Ticks;

    private static PropertyInfo? s_topLevelRendererProperty;

    private RenderTargetBitmap? _scratch;
    private SKImage? _backdrop;
    private Vector _controlOffsetPx;
    private Vector _controlSizePx;
    private ulong _lastHash;
    private long _lastCaptureTicksUtc;
    private bool _captureQueued;

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

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static bool GetExcludeFromCapture(Visual element) => element.GetValue(ExcludeFromCaptureProperty);

    public static void SetExcludeFromCapture(Visual element, bool value) =>
        element.SetValue(ExcludeFromCaptureProperty, value);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureRendererSubscription();
        QueueCapture();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachRendererSubscription();
        _backdrop?.Dispose();
        _backdrop = null;
        _scratch?.Dispose();
        _scratch = null;
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        SKImage? backdrop = _backdrop;
        if (backdrop is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        // 只裁剪毛玻璃背景层；内容（模板的 ContentPresenter）由框架独立渲染，不受此 clip 影响。
        CornerRadius corner = CornerRadius;
        bool rounded = corner != default;
        DrawingContext.PushedState clip = rounded
            ? context.PushClip(new RoundedRect(new Rect(Bounds.Size), corner))
            : default;
        try
        {
            context.Custom(new BackdropDrawOperation(
                new Rect(Bounds.Size),
                backdrop,
                _controlOffsetPx,
                _controlSizePx,
                TintColor,
                (float)TintOpacity));
        }
        finally
        {
            if (rounded)
                clip.Dispose();
        }
    }

    // NOTE: SceneInvalidated 是 Avalonia 未公开的渲染失效信号（内容变化才触发），反射订阅以避免恒定
    // 轮询。订阅是一次性反射，事件触发走正常委托调用，无运行时反射开销。
    private void EnsureRendererSubscription()
    {
        if (_sceneInvalidatedHandler is not null)
            return;

        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        object? renderer = GetRenderer(topLevel);
        if (renderer is null)
            return;

        EventInfo? evt = renderer.GetType().GetEvent(
            "SceneInvalidated",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (evt is null)
            return;

        EventHandler<SceneInvalidatedEventArgs> handler = OnSceneInvalidated;
        evt.AddEventHandler(renderer, handler);
        _renderer = renderer;
        _sceneInvalidatedEvent = evt;
        _sceneInvalidatedHandler = handler;
    }

    private void DetachRendererSubscription()
    {
        if (_renderer is not null && _sceneInvalidatedEvent is not null && _sceneInvalidatedHandler is not null)
            _sceneInvalidatedEvent.RemoveEventHandler(_renderer, _sceneInvalidatedHandler);

        _renderer = null;
        _sceneInvalidatedEvent = null;
        _sceneInvalidatedHandler = null;
    }

    private void OnSceneInvalidated(object? sender, SceneInvalidatedEventArgs e)
    {
        if (DateTime.UtcNow.Ticks - _lastCaptureTicksUtc < s_minIntervalTicks)
            return;

        QueueCapture();
    }

    private void QueueCapture()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || _captureQueued)
            return;

        _captureQueued = true;
        topLevel.RequestAnimationFrame(_ =>
        {
            _captureQueued = false;
            try
            {
                Capture();
            }
            catch
            {
            }
        });
    }

    private void Capture()
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        if (this.TransformToVisual(topLevel) is not { } toTopLevel)
            return;

        Rect controlInTop = new Rect(Bounds.Size).TransformToAABB(toTopLevel);
        double scaling = topLevel.RenderScaling;
        double blurMargin = Math.Max(8.0, BlurRadius * 3.0);
        Rect captureRect = controlInTop.Inflate(blurMargin).Intersect(new Rect(topLevel.ClientSize));
        if (captureRect.Width <= 0 || captureRect.Height <= 0)
            return;

        // HACK: 直接用 captureRect 作 clip 重渲到小尺寸 bitmap 会丢失右侧内容（疑似 RenderTargetBitmap
        //       软件渲染 + Window + 负平移 PushTransform 的交互）。重渲整个 topLevel（已验证完整）再
        //       裁出控件区域。根因待查；移入 Huskui 前应确认是否仍需此绕行。
        PixelSize fullPixel = new(
            (int)Math.Ceiling(topLevel.ClientSize.Width * scaling),
            (int)Math.Ceiling(topLevel.ClientSize.Height * scaling));
        if (_scratch is null || _scratch.PixelSize != fullPixel)
        {
            _scratch?.Dispose();
            _scratch = new RenderTargetBitmap(fullPixel, new Vector(96.0 * scaling, 96.0 * scaling));
        }

        using (DrawingContext ctx = _scratch.CreateDrawingContext())
            BackdropVisualRenderer.Render(ctx, topLevel, new Rect(topLevel.ClientSize), new HashSet<Visual> { this });

        int cx = Math.Max(0, (int)Math.Floor(captureRect.X * scaling));
        int cy = Math.Max(0, (int)Math.Floor(captureRect.Y * scaling));
        int cw = Math.Min(fullPixel.Width - cx, (int)Math.Ceiling(captureRect.Width * scaling));
        int ch = Math.Min(fullPixel.Height - cy, (int)Math.Ceiling(captureRect.Height * scaling));
        if (cw <= 0 || ch <= 0)
            return;

        const int bpp = 4;
        int rowBytes = cw * bpp;
        int length = rowBytes * ch;
        byte[] pixels = new byte[length];
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            _scratch.CopyPixels(new PixelRect(cx, cy, cw, ch), handle.AddrOfPinnedObject(), length, rowBytes);
        }
        finally
        {
            handle.Free();
        }

        ulong hash = HashPixels(pixels, cw, ch, rowBytes);
        _lastCaptureTicksUtc = DateTime.UtcNow.Ticks;

        if (_backdrop is not null && hash == _lastHash)
            return;
        _lastHash = hash;

        _controlOffsetPx = new Vector(
            (controlInTop.X - captureRect.X) * scaling,
            (controlInTop.Y - captureRect.Y) * scaling);
        _controlSizePx = new Vector(controlInTop.Width * scaling, controlInTop.Height * scaling);

        // CPU 离屏模糊整张裁出图（含 margin，边缘不衰减），Render 只需直接画这张成品图。
        float sigmaPx = (float)(BlurRadius * scaling);
        SKImageInfo info = new(cw, ch, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKImage raw = SKImage.FromPixelCopy(info, pixels, rowBytes);
        SKImage blurred;
        using (SKSurface surface = SKSurface.Create(info))
        {
            SKCanvas off = surface.Canvas;
            off.Clear(SKColors.Transparent);
            using SKImageFilter blur = SKImageFilter.CreateBlur(sigmaPx, sigmaPx, SKShaderTileMode.Clamp, null);
            using SKPaint paint = new() { ImageFilter = blur, BlendMode = SKBlendMode.Src };
            off.DrawImage(raw, 0, 0, paint);
            off.Flush();
            blurred = surface.Snapshot();
        }

        _backdrop?.Dispose();
        _backdrop = blurred;
        InvalidateVisual();
    }

    private static ulong HashPixels(byte[] pixels, int width, int height, int rowBytes)
    {
        const ulong fnvOffset = 14695981039346656037UL;
        const ulong fnvPrime = 1099511628211UL;
        ulong hash = fnvOffset;
        const int samples = 8;
        int maxX = Math.Max(1, width) - 1;
        int maxY = Math.Max(1, height) - 1;

        for (int sy = 0; sy < samples; sy++)
        {
            int y = (int)((long)sy * maxY / (samples - 1));
            int rowOff = y * rowBytes;
            for (int sx = 0; sx < samples; sx++)
            {
                int x = (int)((long)sx * maxX / (samples - 1));
                int off = rowOff + x * 4;
                for (int i = 0; i < 4; i++)
                    hash = (hash ^ pixels[off + i]) * fnvPrime;
            }
        }

        hash = (hash ^ (ulong)width) * fnvPrime;
        hash = (hash ^ (ulong)height) * fnvPrime;
        return hash;
    }

    private static object? GetRenderer(TopLevel topLevel)
    {
        s_topLevelRendererProperty ??= typeof(TopLevel).GetProperty(
            "Renderer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return s_topLevelRendererProperty?.GetValue(topLevel);
    }

    private sealed class BackdropDrawOperation : ICustomDrawOperation
    {
        private readonly Rect _bounds;
        private readonly SKImage _backdrop;
        private readonly Vector _controlOffsetPx;
        private readonly Vector _controlSizePx;
        private readonly Color _tint;
        private readonly float _tintOpacity;

        public BackdropDrawOperation(
            Rect bounds,
            SKImage backdrop,
            Vector controlOffsetPx,
            Vector controlSizePx,
            Color tint,
            float tintOpacity)
        {
            _bounds = bounds;
            _backdrop = backdrop;
            _controlOffsetPx = controlOffsetPx;
            _controlSizePx = controlSizePx;
            _tint = tint;
            _tintOpacity = tintOpacity;
        }

        public Rect Bounds => _bounds;

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
                return;

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;

            SKRect src = new(
                (float)_controlOffsetPx.X,
                (float)_controlOffsetPx.Y,
                (float)(_controlOffsetPx.X + _controlSizePx.X),
                (float)(_controlOffsetPx.Y + _controlSizePx.Y));
            SKRect dst = new(0f, 0f, (float)_bounds.Width, (float)_bounds.Height);
            if (src.Width <= 0 || src.Height <= 0)
                return;

            canvas.DrawImage(_backdrop, src, dst, new SKSamplingOptions(SKFilterMode.Linear));

            byte a = (byte)Math.Clamp(_tint.A * _tintOpacity, 0, 255);
            using SKPaint tintPaint = new() { Color = new SKColor(_tint.R, _tint.G, _tint.B, a) };
            canvas.DrawRect(dst, tintPaint);
        }
    }

    private static class BackdropVisualRenderer
    {
        public static void Render(DrawingContext context, Visual visual, Rect clipRect, ISet<Visual> excluded)
        {
            if (clipRect.Width <= 0 || clipRect.Height <= 0)
                return;

            using (context.PushTransform(Matrix.CreateTranslation(-clipRect.X, -clipRect.Y)))
            using (context.PushClip(new Rect(clipRect.Size)))
            {
                Render(context, visual, new Rect(visual.Bounds.Size), Matrix.Identity, clipRect, excluded);
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

        private static void Render(
            DrawingContext context,
            Visual visual,
            Rect bounds,
            Matrix parentTransform,
            Rect clipRect,
            ISet<Visual> excluded)
        {
            if (excluded.Contains(visual) || ShouldExclude(visual))
                return;

            if (!visual.IsVisible || visual.Opacity <= 0)
                return;

            Rect rect = new(bounds.Size);
            Matrix transform;
            if (visual.RenderTransform?.Value is { } rt)
            {
                Point origin = visual.RenderTransformOrigin.ToPixels(visual.Bounds.Size);
                Matrix offset = Matrix.CreateTranslation(origin);
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
                Matrix totalTransform = transform * parentTransform;
                Rect visualBounds = rect.TransformToAABB(totalTransform);

                if (visualBounds.Intersects(clipRect))
                    visual.Render(context);

                IReadOnlyList<Visual> children = GetOrderedChildren(visual);

                Rect childClip = clipRect;
                Matrix childParent = totalTransform;
                if (visual.ClipToBounds)
                {
                    childParent = Matrix.Identity;
                    childClip = rect;
                }

                foreach (Visual? child in children)
                    Render(context, child, child.Bounds, childParent, childClip, excluded);
            }
        }

        private static DrawingContext.PushedState? PushClipToBounds(DrawingContext context, Visual visual, Rect rect)
        {
            if (!visual.ClipToBounds)
                return default;

            if (TryGetClipToBoundsRadius(visual, out CornerRadius radius))
                return context.PushClip(new RoundedRect(rect, radius));

            return context.PushClip(rect);
        }

        private static bool TryGetClipToBoundsRadius(Visual visual, out CornerRadius radius)
        {
            radius = default;
            PropertyInfo? prop = visual.GetType().GetProperty(
                "ClipToBoundsRadius",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.PropertyType != typeof(CornerRadius))
                return false;
            if (prop.GetValue(visual) is not CornerRadius value || value == default)
                return false;
            radius = value;
            return true;
        }

        private static IReadOnlyList<Visual> GetOrderedChildren(Visual visual)
        {
            List<Visual>? list = null;
            int? firstZIndex = null;
            bool hasNonUniformZIndex = false;

            foreach (Visual? child in visual.GetVisualChildren())
            {
                list ??= new List<Visual>();
                list.Add(child);
                if (firstZIndex is null)
                    firstZIndex = child.ZIndex;
                else if (child.ZIndex != firstZIndex.Value)
                    hasNonUniformZIndex = true;
            }

            if (list is null || list.Count == 0)
                return Array.Empty<Visual>();

            return hasNonUniformZIndex ? list.OrderBy(x => x.ZIndex).ToArray() : list;
        }
    }
}
