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

        // 拖拽在视口坐标（this）里做 delta，再换算成矩阵平移。
        // 用视口 delta 而非内容 delta：拖拽手感与屏幕 1:1，不受缩放影响。
        var current = e.GetPosition(this);
        var dx = current.X - _lastPointer.X;
        var dy = current.Y - _lastPointer.Y;
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

    private void ApplyTransform()
    {
        if (Content is Visual content)
        {
            content.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
            content.RenderTransform = new MatrixTransform { Matrix = _matrix };
        }
    }
}
