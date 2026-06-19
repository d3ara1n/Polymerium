using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Polymerium.Avalonia.Controls;

public partial class ZoomView : ContentControl
{
    private Matrix _matrix = Matrix.Identity;
    private Point _lastPointer;
    private bool _isPanning;
    private bool _fitted;
    private Size _contentSize;
    private Size _viewportSize;

    private ScrollBar? _hScrollBar;
    private ScrollBar? _vScrollBar;
    private ZoomMinimap? _minimap;
    private bool _suppressScrollSync;
    private bool _suppressZoomSync;

    public static readonly StyledProperty<double> ZoomSpeedProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(ZoomSpeed), 1.2);

    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(MinZoom), 0.1);

    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(MaxZoom), 10.0);

    public static readonly StyledProperty<Size> ContentSizeProperty =
        AvaloniaProperty.Register<ZoomView, Size>(nameof(ContentSize));

    public static readonly StyledProperty<Rect> ViewportRectProperty =
        AvaloniaProperty.Register<ZoomView, Rect>(nameof(ViewportRect));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(Zoom));

    public static readonly StyledProperty<double> EffectiveMinZoomValueProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(EffectiveMinZoomValue));

    public double ZoomSpeed
    {
        get => GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    public Size ContentSize
    {
        get => GetValue(ContentSizeProperty);
        private set => SetValue(ContentSizeProperty, value);
    }

    public Rect ViewportRect
    {
        get => GetValue(ViewportRectProperty);
        private set => SetValue(ViewportRectProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public double EffectiveMinZoomValue
    {
        get => GetValue(EffectiveMinZoomValueProperty);
        private set => SetValue(EffectiveMinZoomValueProperty, value);
    }

    public double ZoomX => _matrix.M11;

    public double ZoomY => _matrix.M22;

    public ZoomView()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    #region Template

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _hScrollBar = e.NameScope.Find<ScrollBar>("PART_HScrollBar");
        _vScrollBar = e.NameScope.Find<ScrollBar>("PART_VScrollBar");
        _minimap = e.NameScope.Find<ZoomMinimap>("PART_Minimap");

        if (_hScrollBar != null)
        {
            _hScrollBar.ValueChanged += OnHScrollBarValueChanged;
        }

        if (_vScrollBar != null)
        {
            _vScrollBar.ValueChanged += OnVScrollBarValueChanged;
        }

        if (_minimap != null)
        {
            _minimap.ViewportChanged += OnMinimapViewportChanged;
        }
    }

    #endregion

    #region Layout

    protected override Size MeasureOverride(Size availableSize)
    {
        _viewportSize = availableSize;
        if (Content is Layoutable content)
        {
            content.Measure(Size.Infinity);
            _contentSize = content.DesiredSize;
        }
        return availableSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        _viewportSize = finalSize;

        if (!_fitted && IsContentValid && _viewportSize.Width > 0)
        {
            FitToContent();
            _fitted = true;
        }
        else
        {
            Commit();
        }

        return size;
    }

    private bool IsContentValid => _contentSize.Width > 0 && _contentSize.Height > 0;

    private double EffectiveMinZoom()
    {
        if (!IsContentValid || _viewportSize.Width <= 0 || _viewportSize.Height <= 0)
        {
            return MinZoom;
        }

        var fitFloor = Math.Min(
            1.0,
            Math.Min(_viewportSize.Width / _contentSize.Width, _viewportSize.Height / _contentSize.Height)
        );
        return Math.Max(MinZoom, fitFloor);
    }

    private void ClampMatrix()
    {
        if (!IsContentValid || _viewportSize.Width <= 0)
        {
            return;
        }

        var scale = _matrix.M11;
        var scaledW = _contentSize.Width * scale;
        var scaledH = _contentSize.Height * scale;
        var tx = scaledW > _viewportSize.Width
            ? Math.Clamp(_matrix.M31, _viewportSize.Width - scaledW, 0)
            : (_viewportSize.Width - scaledW) / 2;
        var ty = scaledH > _viewportSize.Height
            ? Math.Clamp(_matrix.M32, _viewportSize.Height - scaledH, 0)
            : (_viewportSize.Height - scaledH) / 2;
        _matrix = new Matrix(scale, 0, 0, scale, tx, ty);
    }

    private void Commit()
    {
        ClampMatrix();
        ApplyTransform();
        UpdateExposed();
    }

    #endregion

    #region Pointer

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled || Content is not Visual content)
        {
            return;
        }

        var origin = e.GetPosition(content);
        var delta = e.Delta.Y;
        if (Math.Abs(delta) < 0.001)
        {
            return;
        }

        var factor = delta > 0 ? ZoomSpeed : 1.0 / ZoomSpeed;
        ZoomAt(factor, origin.X, origin.Y);
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || Content is not Visual)
        {
            return;
        }

        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsMiddleButtonPressed)
        {
            return;
        }

        _lastPointer = e.GetPosition(this);
        _isPanning = true;
        e.Pointer.Capture(this);
        e.Handled = true;
        Cursor = new Cursor(StandardCursorType.SizeAll);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isPanning)
        {
            return;
        }

        var current = e.GetPosition(this);
        var dx = (current.X - _lastPointer.X) / ZoomX;
        var dy = (current.Y - _lastPointer.Y) / ZoomY;
        _lastPointer = current;

        _matrix = Matrix.CreateTranslation(dx, dy) * _matrix;
        Commit();
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        e.Pointer.Capture(null);
        Cursor = null;
        e.Handled = true;
    }

    #endregion

    #region Zoom

    private Point ViewportCenterContent =>
        _matrix.M11 == 0
            ? default
            : new Point(
                (-_matrix.M31 + _viewportSize.Width / 2) / _matrix.M11,
                (-_matrix.M32 + _viewportSize.Height / 2) / _matrix.M11
            );

    /// <summary>以内容坐标 (cx, cy) 为锚点，按 factor 缩放，结果 clamp 到 [EffectiveMinZoom, MaxZoom] 与画布边缘内。</summary>
    public void ZoomAt(double factor, double cx, double cy) =>
        ApplyScaleAt(_matrix.M11 * factor, cx, cy);

    public void ZoomIn()
    {
        var c = ViewportCenterContent;
        ZoomAt(ZoomSpeed, c.X, c.Y);
    }

    public void ZoomOut()
    {
        var c = ViewportCenterContent;
        ZoomAt(1.0 / ZoomSpeed, c.X, c.Y);
    }

    /// <summary>以视口中心为锚点缩放到指定值（滑条语义）。</summary>
    private void ZoomToValue(double value)
    {
        var c = ViewportCenterContent;
        ApplyScaleAt(value, c.X, c.Y);
    }

    private void ApplyScaleAt(double targetScale, double cx, double cy)
    {
        if (!IsContentValid || _viewportSize.Width <= 0)
        {
            return;
        }

        var currentScale = _matrix.M11;
        var clampedScale = Math.Clamp(targetScale, EffectiveMinZoom(), MaxZoom);
        if (Math.Abs(clampedScale - currentScale) < 1e-9)
        {
            return;
        }

        var factor = clampedScale / currentScale;
        var scaleAt = new Matrix(factor, 0, 0, factor, cx - factor * cx, cy - factor * cy);
        _matrix = scaleAt * _matrix;
        Commit();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ZoomProperty && !_suppressZoomSync)
        {
            ZoomToValue(Zoom);
        }
    }

    #endregion

    #region Fit

    public void FitToContent()
    {
        if (!IsContentValid || _viewportSize.Width <= 0)
        {
            return;
        }

        var coverScale = Math.Max(
            _viewportSize.Width / _contentSize.Width,
            _viewportSize.Height / _contentSize.Height
        );
        var scale = Math.Clamp(coverScale, EffectiveMinZoom(), MaxZoom);
        var scaledW = _contentSize.Width * scale;
        var scaledH = _contentSize.Height * scale;
        // 居中而非 ClampMatrix 的顶端对齐：cover-fit 下溢出维要居中显示，
        // 但 ClampMatrix 会把初始 tx=0 夹到区间边界 [vp-scaled, 0] 的 0 端（顶端）。
        var tx = (_viewportSize.Width - scaledW) / 2;
        var ty = (_viewportSize.Height - scaledH) / 2;
        _matrix = new Matrix(scale, 0, 0, scale, tx, ty);
        ApplyTransform();
        UpdateExposed();
    }

    public void FitToAll()
    {
        if (!IsContentValid || _viewportSize.Width <= 0)
        {
            return;
        }

        _matrix = new Matrix(EffectiveMinZoom(), 0, 0, EffectiveMinZoom(), 0, 0);
        Commit();
    }

    public void Reset()
    {
        _matrix = Matrix.Identity;
        Commit();
    }

    #endregion

    #region Presentation

    private void ApplyTransform()
    {
        if (Content is Visual content)
        {
            content.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
            content.RenderTransform = new MatrixTransform { Matrix = _matrix };
        }
    }

    /// <summary>当前可视区域在 Content 坐标系下的矩形 = (-translation / scale, viewportSize / scale)。</summary>
    private Rect ComputeViewportRect()
    {
        if (!IsContentValid || _matrix.M11 == 0)
        {
            return default;
        }

        var scale = _matrix.M11;
        return new Rect(
            -_matrix.M31 / scale,
            -_matrix.M32 / scale,
            _viewportSize.Width / scale,
            _viewportSize.Height / scale
        );
    }

    private void UpdateExposed()
    {
        if (!Equals(ContentSize, _contentSize))
        {
            ContentSize = _contentSize;
        }

        var effMin = EffectiveMinZoom();
        if (!Equals(EffectiveMinZoomValue, effMin))
        {
            _suppressZoomSync = true;
            try
            {
                EffectiveMinZoomValue = effMin;
            }
            finally
            {
                _suppressZoomSync = false;
            }
        }

        if (!IsContentValid || _matrix.M11 == 0)
        {
            return;
        }

        var rect = ComputeViewportRect();
        if (!Equals(ViewportRect, rect))
        {
            ViewportRect = rect;
        }

        var scale = _matrix.M11;
        _suppressZoomSync = true;
        try
        {
            if (!Equals(Zoom, scale))
            {
                Zoom = scale;
            }
        }
        finally
        {
            _suppressZoomSync = false;
        }

        UpdateScrollBars();
    }

    #endregion

    #region ScrollBar 同步

    private void UpdateScrollBars()
    {
        if (_suppressScrollSync || _hScrollBar == null || _vScrollBar == null)
        {
            return;
        }

        var scale = _matrix.M11;
        var scaledW = _contentSize.Width * scale;
        var scaledH = _contentSize.Height * scale;

        _suppressScrollSync = true;
        try
        {
            var hMax = Math.Max(0, scaledW - _viewportSize.Width);
            _hScrollBar.Maximum = hMax;
            _hScrollBar.ViewportSize = _viewportSize.Width;
            _hScrollBar.Value = Math.Clamp(-_matrix.M31, 0, hMax);
            _hScrollBar.IsVisible = hMax > 0;

            var vMax = Math.Max(0, scaledH - _viewportSize.Height);
            _vScrollBar.Maximum = vMax;
            _vScrollBar.ViewportSize = _viewportSize.Height;
            _vScrollBar.Value = Math.Clamp(-_matrix.M32, 0, vMax);
            _vScrollBar.IsVisible = vMax > 0;
        }
        finally
        {
            _suppressScrollSync = false;
        }
    }

    private void OnHScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_suppressScrollSync)
        {
            return;
        }

        _suppressScrollSync = true;
        try
        {
            _matrix = new Matrix(_matrix.M11, 0, 0, _matrix.M22, -e.NewValue, _matrix.M32);
            ApplyTransform();
            UpdateViewportRectOnly();
        }
        finally
        {
            _suppressScrollSync = false;
        }
    }

    private void OnVScrollBarValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_suppressScrollSync)
        {
            return;
        }

        _suppressScrollSync = true;
        try
        {
            _matrix = new Matrix(_matrix.M11, 0, 0, _matrix.M22, _matrix.M31, -e.NewValue);
            ApplyTransform();
            UpdateViewportRectOnly();
        }
        finally
        {
            _suppressScrollSync = false;
        }
    }

    private void UpdateViewportRectOnly()
    {
        var rect = ComputeViewportRect();
        if (rect != default && !Equals(ViewportRect, rect))
        {
            ViewportRect = rect;
        }
    }

    #endregion

    #region Minimap 桥接

    private void OnMinimapViewportChanged(object? sender, Point contentTopLeft)
    {
        if (!IsContentValid)
        {
            return;
        }

        var scale = _matrix.M11;
        _matrix = new Matrix(scale, 0, 0, scale, -contentTopLeft.X * scale, -contentTopLeft.Y * scale);
        Commit();
    }

    #endregion
}
