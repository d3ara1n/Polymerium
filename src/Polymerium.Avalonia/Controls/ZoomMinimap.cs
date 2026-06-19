using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     依赖图等缩放视图的小地图（二维示意，非缩略图）。
///     把整张画布（<see cref="ContentSize" />）按 contain fit 缩进自身边界，
///     在其上绘制当前 <see cref="ViewportRect" /> 对应的可视框；
///     点击/拖拽时把指针位置映射回画布坐标，通过 <see cref="ViewportChanged" /> 通知宿主平移。
///     自绘 Control，参考 <see cref="DiffOverviewBar" />（后者仅垂直一维）。
/// </summary>
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
        // 任一属性变化都触发重绘
        AffectsRender<ZoomMinimap>(ContentSizeProperty, ViewportRectProperty);
    }

    /// <summary>画布（Content 自然）尺寸。</summary>
    public Size ContentSize
    {
        get => GetValue(ContentSizeProperty);
        set => SetValue(ContentSizeProperty, value);
    }

    /// <summary>当前可视区域在画布坐标系中的矩形。</summary>
    public Rect ViewportRect
    {
        get => GetValue(ViewportRectProperty);
        set => SetValue(ViewportRectProperty, value);
    }

    /// <summary>拖拽/点击时触发，参数为目标可视区域左上角在画布坐标系的点。</summary>
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

        // 画布示意区背景
        context.DrawRectangle(_trackBrush, null, new RoundedRect(content, 2));

        // 可视框：ViewportRect * scale + content 左上偏移
        var vp = ViewportRect;
        var thumb = new Rect(
            content.X + vp.X * scale,
            content.Y + vp.Y * scale,
            Math.Max(MIN_THUMB, vp.Width * scale),
            Math.Max(MIN_THUMB, vp.Height * scale)
        );
        // 把可视框裁到示意区内（缩到全视图时可视框会超出）
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

    /// <summary>
    ///     把指针在小地图上的位置映射为「可视区域左上角应在的画布坐标」，
    ///     让点击位置对齐到可视框中心（更符合直觉）。
    /// </summary>
    private void RequestViewport(PointerEventArgs e)
    {
        var (content, scale) = ComputeContentArea(Bounds.Width, Bounds.Height);
        if (scale <= 0)
        {
            return;
        }

        var pos = e.GetPosition(this);
        var vp = ViewportRect;
        // 指针相对示意区左上角 / scale = 画布坐标；再减半个可视框让指针居中
        var cx = (pos.X - content.X) / scale - vp.Width / 2;
        var cy = (pos.Y - content.Y) / scale - vp.Height / 2;
        // clamp 到画布范围（与 ZoomView.ClampMatrix 一致的可滚动区间）
        var maxX = Math.Max(0, ContentSize.Width - vp.Width);
        var maxY = Math.Max(0, ContentSize.Height - vp.Height);
        ViewportChanged?.Invoke(this, new Point(Math.Clamp(cx, 0, maxX), Math.Clamp(cy, 0, maxY)));
    }

    /// <summary>
    ///     计算画布示意区在控件边界内的矩形与缩放比（contain fit）。
    /// </summary>
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
        // 居中
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
