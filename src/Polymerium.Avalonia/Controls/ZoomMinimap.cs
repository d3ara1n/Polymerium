using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace Polymerium.Avalonia.Controls;

public class ZoomMinimap : Control
{
    private const double MIN_THUMB = 4.0;

    public static readonly StyledProperty<Size> ContentSizeProperty =
        AvaloniaProperty.Register<ZoomMinimap, Size>(nameof(ContentSize));

    public static readonly StyledProperty<Rect> ViewportRectProperty =
        AvaloniaProperty.Register<ZoomMinimap, Rect>(nameof(ViewportRect));

    private IBrush? _trackBrush;
    private IBrush? _thumbBrush;
    private IPen? _thumbPen;
    private ThemeVariant? _theme;
    private bool _isDragging;

    static ZoomMinimap()
    {
        AffectsRender<ZoomMinimap>(ContentSizeProperty, ViewportRectProperty);
    }

    public Size ContentSize
    {
        get => GetValue(ContentSizeProperty);
        set => SetValue(ContentSizeProperty, value);
    }

    public Rect ViewportRect
    {
        get => GetValue(ViewportRectProperty);
        set => SetValue(ViewportRectProperty, value);
    }

    public event EventHandler<Point>? ViewportChanged;

    public override void Render(DrawingContext context)
    {
        EnsureBrushes();
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
        {
            return;
        }

        var (content, scale) = ComputeContentArea(w, h);
        if (scale <= 0)
        {
            return;
        }

        context.DrawRectangle(_trackBrush, null, new RoundedRect(content, 2));

        var vp = ViewportRect;
        var thumb = new Rect(
            content.X + vp.X * scale,
            content.Y + vp.Y * scale,
            Math.Max(MIN_THUMB, vp.Width * scale),
            Math.Max(MIN_THUMB, vp.Height * scale)
        );
        thumb = thumb.Intersect(content);
        if (thumb.Width > 0 && thumb.Height > 0)
        {
            context.DrawRectangle(_thumbBrush, null, new RoundedRect(thumb, 2));
            if (_thumbPen != null)
            {
                context.DrawRectangle(null, _thumbPen, new RoundedRect(thumb, 2));
            }
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        e.Pointer.Capture(this);
        RequestViewport(e);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            RequestViewport(e);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void RequestViewport(PointerEventArgs e)
    {
        var (content, scale) = ComputeContentArea(Bounds.Width, Bounds.Height);
        if (scale <= 0)
        {
            return;
        }

        var pos = e.GetPosition(this);
        var vp = ViewportRect;
        var cx = (pos.X - content.X) / scale - vp.Width / 2;
        var cy = (pos.Y - content.Y) / scale - vp.Height / 2;
        var maxX = Math.Max(0, ContentSize.Width - vp.Width);
        var maxY = Math.Max(0, ContentSize.Height - vp.Height);
        ViewportChanged?.Invoke(this, new Point(Math.Clamp(cx, 0, maxX), Math.Clamp(cy, 0, maxY)));
    }

    private (Rect area, double scale) ComputeContentArea(double w, double h)
    {
        var cw = ContentSize.Width;
        var ch = ContentSize.Height;
        if (cw <= 0 || ch <= 0)
        {
            return (default, 0);
        }

        var scale = Math.Min(w / cw, h / ch);
        var areaW = cw * scale;
        var areaH = ch * scale;
        var x = (w - areaW) / 2;
        var y = (h - areaH) / 2;
        return (new Rect(x, y, areaW, areaH), scale);
    }

    private void EnsureBrushes()
    {
        if (_theme == Application.Current?.ActualThemeVariant)
        {
            return;
        }

        _theme = Application.Current?.ActualThemeVariant;
        _trackBrush = TryBrush(
            "ControlTranslucentHalfBackgroundBrush",
            new SolidColorBrush(Color.FromArgb(40, 0x80, 0x80, 0x80))
        );
        _thumbBrush = TryBrush(
            "ControlAccentTranslucentHalfBackgroundBrush",
            new SolidColorBrush(Color.FromArgb(80, 0x00, 0x90, 0xFF))
        );
        var border = TryBrush(
            "ControlAccentBorderBrush",
            new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x90, 0xFF))
        );
        _thumbPen = new Pen(border, 1.0);
    }

    private static IBrush TryBrush(string key, IBrush fallback) =>
        Application.Current?.TryGetResource(key, null, out var res) == true && res is IBrush b
            ? b
            : fallback;
}
