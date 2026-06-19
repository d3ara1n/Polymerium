using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     可缩放可平移的内容容器。滚轮缩放（以鼠标位置为锚点），中键拖拽平移。
///     使用 child 的 RenderTransform + 单个 Matrix 表达变换，不依赖 ScrollViewer/InteractionTracker。
///     参考 PanAndZoom 的 ZoomBorder 架构，按本项目需求裁剪（无限平移、中键拖拽）。
/// </summary>
public class ZoomView : ContentControl
{
    private Matrix _matrix = Matrix.Identity;
    private Point _lastPointer;
    private bool _isPanning;

    public static readonly StyledProperty<double> ZoomSpeedProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(ZoomSpeed), 1.2);

    public static readonly StyledProperty<double> MinZoomProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(MinZoom), 0.1);

    public static readonly StyledProperty<double> MaxZoomProperty =
        AvaloniaProperty.Register<ZoomView, double>(nameof(MaxZoom), 10.0);

    /// <summary>
    ///     每次滚轮的缩放倍率，>1（默认 1.2）。
    /// </summary>
    public double ZoomSpeed
    {
        get => GetValue(ZoomSpeedProperty);
        set => SetValue(ZoomSpeedProperty, value);
    }

    /// <summary>
    ///     最小缩放比例（默认 0.1）。
    /// </summary>
    public double MinZoom
    {
        get => GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    /// <summary>
    ///     最大缩放比例（默认 10）。
    /// </summary>
    public double MaxZoom
    {
        get => GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
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

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled || Content is not Visual content)
        {
            return;
        }

        // 直接滚轮缩放（不要求修饰键），以鼠标位置（内容坐标）为锚点。
        var origin = e.GetPosition(content);
        var delta = e.Delta.Y;
        if (Math.Abs(delta) < 0.001)
        {
            return;
        }

        // 滚轮向上（delta > 0）放大，向下（delta < 0）缩小。
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
        // 中键拖拽平移（避免和内容本身的左键交互冲突，如未来节点可选）。
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

        // 拖拽在视口坐标（this）里取 delta，再除以当前缩放，使内容坐标跟手：
        // 缩小（scale<1）时平移量被放大，浏览全局更省力；放大时精细微调。
        // 若用裸 delta 则是屏幕跟手，缩小后图小、相对位移太小，拖动费力。
        var current = e.GetPosition(this);
        var dx = (current.X - _lastPointer.X) / ZoomX;
        var dy = (current.Y - _lastPointer.Y) / ZoomY;
        _lastPointer = current;

        // TranslatePrepend = Translate(dx,dy) * matrix
        _matrix = Matrix.CreateTranslation(dx, dy) * _matrix;
        ApplyTransform();
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

    /// <summary>
    ///     以内容坐标 (cx, cy) 为锚点缩放。M_new = ScaleAt(s, s, cx, cy) * M。
    /// </summary>
    public void ZoomAt(double factor, double cx, double cy)
    {
        var newZoomX = _matrix.M11 * factor;
        var newZoomY = _matrix.M22 * factor;
        if (newZoomX < MinZoom || newZoomX > MaxZoom || newZoomY < MinZoom || newZoomY > MaxZoom)
        {
            return;
        }

        // ScaleAt(s,s,cx,cy) = { s,0,0,s, cx-s*cx, cy-s*cy }
        var scaleAt = new Matrix(factor, 0, 0, factor, cx - factor * cx, cy - factor * cy);
        _matrix = scaleAt * _matrix;
        ApplyTransform();
    }

    /// <summary>
    ///     重置变换到单位矩阵。
    /// </summary>
    public void Reset()
    {
        _matrix = Matrix.Identity;
        ApplyTransform();
    }

    /// <summary>
    ///     自适应内容：计算使 Content 充满并居中于视口所需的缩放与平移，一次性应用。
    ///     需在 Content 完成布局（Bounds 有效）后调用，否则 no-op。
    /// </summary>
    public void FitToContent()
    {
        if (Content is not Visual content)
        {
            return;
        }

        var contentBounds = content.Bounds;
        var viewport = Bounds;
        if (contentBounds.Width <= 0 || contentBounds.Height <= 0
            || viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var scaleX = viewport.Width / contentBounds.Width;
        var scaleY = viewport.Height / contentBounds.Height;
        var scale = Math.Min(scaleX, scaleY);
        // 不放大超过 1，避免对小图过度拉伸；下限受 MinZoom 约束。
        scale = Math.Clamp(scale, MinZoom, Math.Min(1.0, MaxZoom));

        // 内容本地 (0,0) 经 M=(s,0,0,s,tx,ty) 映射到 Bounds.Position + (tx,ty)。
        // 要内容居中于视口：tx = (vp.W - s*cb.W)/2 - cb.X
        var tx = (viewport.Width - scale * contentBounds.Width) / 2 - contentBounds.X;
        var ty = (viewport.Height - scale * contentBounds.Height) / 2 - contentBounds.Y;
        _matrix = new Matrix(scale, 0, 0, scale, tx, ty);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        if (Content is Visual content)
        {
            content.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
            content.RenderTransform = new MatrixTransform { Matrix = _matrix };
        }
    }
}
