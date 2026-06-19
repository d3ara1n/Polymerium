using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     有界可缩放可平移内容容器。
///     画布 = Content 的自然尺寸（GraphPanel 的 MSAGL 布局尺寸），平移被 clamp 在画布边缘内，
///     缩放下限动态计算为「可视区域已看到整张图」的临界值（contain fit），
///     上限由 <see cref="MaxZoom" /> 约束。
///     滚轮缩放以鼠标位置为锚点，中键拖拽平移按内容坐标跟手（delta 除以当前缩放）。
///     通过 <see cref="ContentSize" /> 与 <see cref="ViewportRect" /> 暴露画布尺寸与可视区域，
///     供小地图 / 滚动条等辅助控件绑定。
/// </summary>
public class ZoomView : ContentControl
{
    private Matrix _matrix = Matrix.Identity;
    private Point _lastPointer;
    private bool _isPanning;
    private bool _fitted;
    private Size _contentSize;
    private Size _viewportSize;

    // 模板子控件引用
    private ContentPresenter? _presenter;
    private ScrollBar? _hScrollBar;
    private ScrollBar? _vScrollBar;
    // 防止滚动条 Value 与 Matrix 双向同步时递归
    private bool _suppressScrollSync;

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

    /// <summary>
    ///     每次滚轮的缩放倍率，&gt;1（默认 1.2）。
    /// </summary>
    public double ZoomSpeed
    {
        get => GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    /// <summary>
    ///     用户设定的绝对缩放下限（默认 0.1）。实际下限取 max(this, 动态 fit 下限)。
    /// </summary>
    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    /// <summary>
    ///     缩放上限（默认 10）。
    /// </summary>
    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    /// <summary>
    ///     画布（Content 自然）尺寸，供小地图等控件绑定。
    /// </summary>
    public Size ContentSize
    {
        get => GetValue(ContentSizeProperty);
        private set => SetValue(ContentSizeProperty, value);
    }

    /// <summary>
    ///     当前可视区域在画布（Content）坐标系中的矩形，供小地图 / 滚动条绑定。
    /// </summary>
    public Rect ViewportRect
    {
        get => GetValue(ViewportRectProperty);
        private set => SetValue(ViewportRectProperty, value);
    }

    /// <summary>
    ///     当前水平缩放比例。
    /// </summary>
    public double ZoomX => _matrix.M11;

    /// <summary>
    ///     当前垂直缩放比例。
    /// </summary>
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
        _presenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _hScrollBar = e.NameScope.Find<ScrollBar>("PART_HScrollBar");
        _vScrollBar = e.NameScope.Find<ScrollBar>("PART_VScrollBar");

        if (_hScrollBar != null)
        {
            _hScrollBar.ValueChanged += OnHScrollBarValueChanged;
        }

        if (_vScrollBar != null)
        {
            _vScrollBar.ValueChanged += OnVScrollBarValueChanged;
        }
    }

    #endregion

    #region Layout

    protected override Size MeasureOverride(Size availableSize)
    {
        // ZoomView 自身占满父给的视口，但 content 必须用 infinity measure，
        // 否则 Avalonia 会把 content.DesiredSize 截断到 availableSize——
        // 对 GraphPanel 这种自然尺寸远大于视口的控件，会丢掉真实画布尺寸，
        // 导致 fit 下限计算成 1×、只能看到左上角一小块。
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
        // 注意：不在这里读 content.DesiredSize——base.ArrangeOverride 内部会用 finalSize
        // 重新 measure content，把 DesiredSize 截断回视口尺寸，会覆盖掉 MeasureOverride 里
        // 用 Size.Infinity 拿到的真实画布尺寸。_contentSize 只由 MeasureOverride 维护。

        if (!_fitted && IsContentValid && _viewportSize.Width > 0)
        {
            // 首次拿到有效画布 + 视口尺寸时做一次自适应（cover fit）。
            FitToContent();
            _fitted = true;
        }
        else
        {
            // 视口尺寸变化（窗口缩放等）后，按新视口重新 clamp 并刷新暴露属性。
            ClampMatrix();
            ApplyTransform();
            UpdateExposed();
        }

        return size;
    }

    private bool IsContentValid => _contentSize.Width > 0 && _contentSize.Height > 0;

    /// <summary>
    ///     动态缩放下限：可视区域已看到整张图时的 contain fit 比例。
    ///     取 min(vp.W/content.W, vp.H/content.H)——较短那维刚好充满视口、较长那维全可见，
    ///     即「宽和长谁先全部可见就停在谁那」。再与 1.0 取小（图比视口小时不放大，
    ///     自然尺寸即已全可见），最后与用户设定的 <see cref="MinZoom" /> 取大。
    /// </summary>
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

    /// <summary>
    ///     将矩阵的平移分量 clamp 到画布边缘内：
    ///     该维缩放后 &gt; 视口 → 可滚动，平移被夹在 [vp - scaled, 0]；
    ///     该维缩放后 ≤ 视口 → 不可滚动，居中。
    /// </summary>
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

        // 拖拽在视口坐标里取 delta，再除以当前缩放，使内容坐标跟手：
        // 缩小（scale<1）时平移量被放大，浏览全局更省力；放大时精细微调。
        var current = e.GetPosition(this);
        var dx = (current.X - _lastPointer.X) / ZoomX;
        var dy = (current.Y - _lastPointer.Y) / ZoomY;
        _lastPointer = current;

        _matrix = Matrix.CreateTranslation(dx, dy) * _matrix;
        ClampMatrix();
        ApplyTransform();
        UpdateExposed();
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

    #region Public API

    /// <summary>
    ///     以内容坐标 (cx, cy) 为锚点缩放。M_new = ScaleAt(s, s, cx, cy) * M。
    ///     结果被 clamp 到 [EffectiveMinZoom, MaxZoom] 与画布边缘内。
    /// </summary>
    public void ZoomAt(double factor, double cx, double cy)
    {
        var currentScale = _matrix.M11;
        var newScale = currentScale * factor;
        var clampedScale = Math.Clamp(newScale, EffectiveMinZoom(), MaxZoom);
        if (Math.Abs(clampedScale - currentScale) < 1e-9)
        {
            return;
        }

        var actualFactor = clampedScale / currentScale;
        var scaleAt = new Matrix(
            actualFactor,
            0,
            0,
            actualFactor,
            cx - actualFactor * cx,
            cy - actualFactor * cy
        );
        _matrix = scaleAt * _matrix;
        ClampMatrix();
        ApplyTransform();
        UpdateExposed();
    }

    /// <summary>
    ///     自适应（cover fit）：较短维充满视口、较长维溢出可滚动。
    ///     例：纵向长条内容 → 宽充满视口、高溢出，用户上下滚动，缩放不致太小、看得清。
    ///     缩放比例受 <see cref="EffectiveMinZoom" /> 下限约束，不会比 contain fit 更小。
    ///     溢出维初始居中（而非顶端对齐），即「自适应做到溢出那边居中」。
    ///     即「回到自适应」按钮的行为。
    /// </summary>
    public void FitToContent()
    {
        if (!IsContentValid || _viewportSize.Width <= 0)
        {
            return;
        }

        // cover fit：取较大比例 → 短维充满、长维溢出。
        var coverScale = Math.Max(
            _viewportSize.Width / _contentSize.Width,
            _viewportSize.Height / _contentSize.Height
        );
        // 不超过 MaxZoom，不低于 contain fit 下限（避免缩到整图可见还小）。
        var scale = Math.Clamp(coverScale, EffectiveMinZoom(), MaxZoom);
        var scaledW = _contentSize.Width * scale;
        var scaledH = _contentSize.Height * scale;
        // 短维充满（scaled≈vp）→ 居中平移 = 0；长维溢出（scaled>vp）→ 居中平移 = (vp-scaled)/2。
        // 与 ClampMatrix 的「顶端对齐滚动」不同，这里初始把溢出维居中显示。
        var tx = (_viewportSize.Width - scaledW) / 2;
        var ty = (_viewportSize.Height - scaledH) / 2;
        _matrix = new Matrix(scale, 0, 0, scale, tx, ty);
        // 不走 ClampMatrix：它会夹到 [vp-scaled, 0] 顶端对齐，覆盖掉居中意图。
        ApplyTransform();
        UpdateExposed();
    }

    /// <summary>
    ///     重置到 1× 并 clamp（不会突破动态下限）。
    /// </summary>
    public void Reset()
    {
        _matrix = Matrix.Identity;
        ClampMatrix();
        ApplyTransform();
        UpdateExposed();
    }

    /// <summary>
    ///     全视图（contain fit）：整张图刚好全部进入视口——较短维充满、较长维留白居中。
    ///     缩放比例 = <see cref="EffectiveMinZoom" />，即缩放下限本身。
    /// </summary>
    public void FitToAll()
    {
        if (!IsContentValid || _viewportSize.Width <= 0)
        {
            return;
        }

        var scale = EffectiveMinZoom();
        _matrix = new Matrix(scale, 0, 0, scale, 0, 0);
        ClampMatrix();
        ApplyTransform();
        UpdateExposed();
    }

    #endregion

    private void ApplyTransform()
    {
        // RenderTransform 必须设在 Content（如 GraphPanel）上，而不是 ContentPresenter：
        // ContentPresenter 的 Bounds 是视口大小，对它设变换会让超出视口的 Content 受限于其布局
        // 边界而无法完整缩放。设在 Content 上则 Content 完整渲染自己全部（如 GraphPanel 所有节点），
        // 变换后的像素再由 ZoomView 的 ClipToBounds 裁到视口。
        if (Content is Visual content)
        {
            content.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
            content.RenderTransform = new MatrixTransform { Matrix = _matrix };
        }
    }

    private void UpdateExposed()
    {
        if (!Equals(ContentSize, _contentSize))
        {
            ContentSize = _contentSize;
        }

        if (!IsContentValid || _matrix.M11 == 0)
        {
            return;
        }

        var scale = _matrix.M11;
        var vx = -_matrix.M31 / scale;
        var vy = -_matrix.M32 / scale;
        var vw = _viewportSize.Width / scale;
        var vh = _viewportSize.Height / scale;
        var rect = new Rect(vx, vy, vw, vh);
        if (!Equals(ViewportRect, rect))
        {
            ViewportRect = rect;
        }

        UpdateScrollBars();
    }

    #region ScrollBar 同步

    /// <summary>
    ///     根据 Matrix 与画布/视口尺寸刷新滚动条的 Maximum/ViewportSize/Value/Visibility。
    ///     滚动条 Value 范围 [0, Maximum]，Maximum = 该维缩放后超出视口的部分（可滚动距离）。
    ///     Value=0 对应顶端对齐（tx=0），Value=Maximum 对应底端（tx=vp-scaled）。
    ///     通过 <see cref="_suppressScrollSync" /> 避免与 OnScrollBarValueChanged 递归。
    /// </summary>
    private void UpdateScrollBars()
    {
        if (_suppressScrollSync || _hScrollBar == null || _vScrollBar == null)
        {
            return;
        }

        var scale = _matrix.M11;
        var scaledW = _contentSize.Width * scale;
        var scaledH = _contentSize.Height * scale;

        // 水平：仅当缩放后宽超出视口时可滚动
        var hMax = Math.Max(0, scaledW - _viewportSize.Width);
        _suppressScrollSync = true;
        try
        {
            _hScrollBar.Maximum = hMax;
            _hScrollBar.ViewportSize = _viewportSize.Width;
            _hScrollBar.Value = Math.Clamp(-_matrix.M31, 0, hMax);
            _hScrollBar.IsVisible = hMax > 0;

            // 垂直
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

        // Value (0~Maximum) → 平移 tx = -Value（顶端对齐为 0，底端为 vp-scaled）
        _suppressScrollSync = true;
        try
        {
            _matrix = new Matrix(_matrix.M11, 0, 0, _matrix.M22, -e.NewValue, _matrix.M32);
            ApplyTransform();
            // 视口矩形也要刷新（平移变了），但不再调 UpdateScrollBars（会递归）
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

    /// <summary>
    ///     仅刷新 ViewportRect（不碰滚动条），供滚动条拖动回调用，避免递归。
    /// </summary>
    private void UpdateViewportRectOnly()
    {
        if (!IsContentValid || _matrix.M11 == 0)
        {
            return;
        }

        var scale = _matrix.M11;
        var vx = -_matrix.M31 / scale;
        var vy = -_matrix.M32 / scale;
        var vw = _viewportSize.Width / scale;
        var vh = _viewportSize.Height / scale;
        var rect = new Rect(vx, vy, vw, vh);
        if (!Equals(ViewportRect, rect))
        {
            ViewportRect = rect;
        }
    }

    #endregion
}
