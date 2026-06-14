using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Controls;

public class DiffOverviewBar : Control
{
    public const double DEFAULT_WIDTH = 20.0;
    private const double MARKER_WIDTH = 5.0;
    private const double MIN_MARKER_HEIGHT = 1.0;
    private const double MIN_THUMB_HEIGHT = 12.0;

    public static readonly StyledProperty<IReadOnlyList<DiffMarker>?> MarkersProperty =
        AvaloniaProperty.Register<DiffOverviewBar, IReadOnlyList<DiffMarker>?>(nameof(Markers));

    public static readonly StyledProperty<double> ViewTopRatioProperty =
        AvaloniaProperty.Register<DiffOverviewBar, double>(nameof(ViewTopRatio));

    public static readonly StyledProperty<double> ViewportRatioProperty =
        AvaloniaProperty.Register<DiffOverviewBar, double>(nameof(ViewportRatio));

    public static readonly StyledProperty<int> TotalLinesProperty =
        AvaloniaProperty.Register<DiffOverviewBar, int>(nameof(TotalLines));

    static DiffOverviewBar()
    {
        AffectsRender<DiffOverviewBar>(
            MarkersProperty,
            ViewTopRatioProperty,
            ViewportRatioProperty,
            TotalLinesProperty
        );
    }

    public IReadOnlyList<DiffMarker>? Markers
    {
        get => GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    public double ViewTopRatio
    {
        get => GetValue(ViewTopRatioProperty);
        set => SetValue(ViewTopRatioProperty, value);
    }

    public double ViewportRatio
    {
        get => GetValue(ViewportRatioProperty);
        set => SetValue(ViewportRatioProperty, value);
    }

    public int TotalLines
    {
        get => GetValue(TotalLinesProperty);
        set => SetValue(TotalLinesProperty, value);
    }

    public event EventHandler<double>? ScrollRequested;

    private IBrush? _trackBrush;
    private IBrush? _thumbBrush;
    private IBrush? _thumbBorderBrush;
    private IBrush? _addedBrush;
    private IBrush? _removedBrush;
    private IBrush? _modifiedBrush;
    private IPen? _thumbPen;
    private ThemeVariant? _theme;

    private bool _isDragging;

    private void EnsureBrushes()
    {
        if (_theme == Application.Current?.ActualThemeVariant)
            return;

        _theme = Application.Current?.ActualThemeVariant;
        _trackBrush = TryBrush(
            "ControlTranslucentHalfBackgroundBrush",
            new SolidColorBrush(Color.FromArgb(40, 0x80, 0x80, 0x80))
        );
        _thumbBrush = TryBrush(
            "ControlTranslucentFullBackgroundBrush",
            new SolidColorBrush(Color.FromArgb(80, 0x80, 0x80, 0x80))
        );
        _thumbBorderBrush = TryBrush(
            "ControlBorderBrush",
            new SolidColorBrush(Color.FromArgb(0xFF, 0x80, 0x80, 0x80))
        );
        _addedBrush = TryBrush(
            "ControlSuccessBackgroundBrush",
            new SolidColorBrush(Color.FromRgb(0x2B, 0x9A, 0x66))
        );
        _removedBrush = TryBrush(
            "ControlDangerBackgroundBrush",
            new SolidColorBrush(Color.FromRgb(0xDC, 0x3E, 0x42))
        );
        _modifiedBrush = TryBrush(
            "ControlAccentBackgroundBrush",
            new SolidColorBrush(Color.FromRgb(0x00, 0x90, 0xFF))
        );
        _thumbPen = new Pen(_thumbBorderBrush, 1.0);
    }

    private static IBrush TryBrush(string key, IBrush fallback) =>
        Application.Current?.TryGetResource(key, null, out var res) == true && res is IBrush b
            ? b
            : fallback;

    private IBrush BrushFor(DiffLineKind kind) =>
        kind switch
        {
            DiffLineKind.Added => _addedBrush!,
            DiffLineKind.Removed => _removedBrush!,
            _ => _modifiedBrush!,
        };

    public override void Render(DrawingContext context)
    {
        EnsureBrushes();
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0)
            return;

        var radius = Math.Min(w, h) * 0.25;

        // track
        context.DrawRectangle(_trackBrush, null, new RoundedRect(new Rect(0, 0, w, h), radius));

        // markers (left edge ticks)
        if (Markers is { Count: > 0 })
        {
            var x = (w - MARKER_WIDTH) / 2.0;
            foreach (var m in Markers)
            {
                var y = m.YRatio * h;
                var markerHeight = Math.Max(MIN_MARKER_HEIGHT, m.HeightRatio * h);
                context.DrawRectangle(
                    BrushFor(m.Kind),
                    null,
                    new Rect(x, y, MARKER_WIDTH, markerHeight)
                );
            }
        }

        // thumb
        var thumbH = Math.Max(MIN_THUMB_HEIGHT, ViewportRatio * h);
        var thumbY = ViewTopRatio * (h - thumbH);
        var thumbRect = new RoundedRect(new Rect(0, thumbY, w, thumbH), radius);
        context.DrawRectangle(_thumbBrush, null, thumbRect);
        if (_thumbPen != null)
            context.DrawRectangle(null, _thumbPen, thumbRect);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _isDragging = true;
        e.Pointer.Capture(this);
        RequestScroll(e);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
            RequestScroll(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
    }

    private void RequestScroll(PointerEventArgs e)
    {
        var pos = e.GetPosition(this);
        var h = Bounds.Height;
        if (h <= 0)
            return;
        // 让点击位置对齐到 thumb 中心，更符合直觉
        var thumbH = Math.Max(MIN_THUMB_HEIGHT, ViewportRatio * h);
        var movable = h - thumbH;
        var ratio = movable > 0 ? (pos.Y - thumbH / 2.0) / movable : 0;
        ScrollRequested?.Invoke(this, Math.Clamp(ratio, 0.0, 1.0));
    }
}
