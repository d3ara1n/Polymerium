using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Polymerium.Avalonia.Controls;

/// <summary>
///     毛玻璃背景控件：在渲染线程截取目标 surface 上已绘制的后方像素，模糊后叠加主题色。
/// </summary>
public class BlurBackdrop : ContentControl
{
    public static readonly StyledProperty<double> BlurRadiusProperty =
        AvaloniaProperty.Register<BlurBackdrop, double>(nameof(BlurRadius), 16.0);

    public static readonly StyledProperty<Color> TintColorProperty =
        AvaloniaProperty.Register<BlurBackdrop, Color>(nameof(TintColor), Colors.Transparent);

    // 拿不到可截取的 Skia surface（离屏中间层、非 Skia 后端）时绘制的纯色底。
    public static readonly StyledProperty<IBrush?> FallbackBrushProperty =
        AvaloniaProperty.Register<BlurBackdrop, IBrush?>(nameof(FallbackBrush));

    // 挂在内置毛玻璃背景层的 overlay（Modal/Dialog/Sidebar/Toast）上，设为 False 时关闭默认毛玻璃并恢复不透明底色，
    // 异型控件可在自身内容里手动放置 BlurBackdrop。
    public static readonly AttachedProperty<bool> UseBlurProperty =
        AvaloniaProperty.RegisterAttached<BlurBackdrop, Control, bool>("UseBlur", true);

    static BlurBackdrop() =>
        AffectsRender<BlurBackdrop>(BlurRadiusProperty, TintColorProperty, CornerRadiusProperty, FallbackBrushProperty);

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

    public IBrush? FallbackBrush
    {
        get => GetValue(FallbackBrushProperty);
        set => SetValue(FallbackBrushProperty, value);
    }

    public static bool GetUseBlur(Control element) => element.GetValue(UseBlurProperty);

    public static void SetUseBlur(Control element, bool value) => element.SetValue(UseBlurProperty, value);

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var fallback = FallbackBrush is ISolidColorBrush solid ? solid.Color : TintColor;
        context.Custom(new BackdropDrawOperation(new(Bounds.Size),
                                                 CornerRadius,
                                                 (float)BlurRadius,
                                                 ToSkColor(TintColor),
                                                 ToSkColor(fallback)));
    }

    private static SKColor ToSkColor(Color color) => new(color.R, color.G, color.B, color.A);

    private sealed class BackdropDrawOperation(
        Rect bounds,
        CornerRadius corners,
        float blurRadius,
        SKColor tint,
        SKColor fallback) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation? other) => false;

        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature)
            {
                return;
            }

            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            // NOTE: 自定义 Skia 绘制不继承父级 PushOpacity，必须手动乘进每个 paint，否则 overlay 淡入淡出时毛玻璃不跟随。
            var alpha = (byte)(Math.Clamp(lease.CurrentOpacity, 0, 1) * 255);
            var rect = new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height);

            using var clip = new SKRoundRect();
            clip.SetRectRadii(rect,
            [
                new((float)corners.TopLeft, (float)corners.TopLeft),
                new((float)corners.TopRight, (float)corners.TopRight),
                new((float)corners.BottomRight, (float)corners.BottomRight),
                new((float)corners.BottomLeft, (float)corners.BottomLeft)
            ]);

            canvas.Save();
            try
            {
                canvas.ClipRoundRect(clip, SKClipOperation.Intersect, true);
                var overlay = TryDrawBackdrop(lease.SkSurface, canvas, rect, alpha) ? tint : fallback;
                using var paint = new SKPaint { Color = overlay.WithAlpha((byte)(overlay.Alpha * alpha / 255)) };
                canvas.DrawRect(rect, paint);
            }
            finally
            {
                canvas.Restore();
            }
        }

        private bool TryDrawBackdrop(SKSurface? surface, SKCanvas canvas, SKRect rect, byte alpha)
        {
            if (surface is null)
            {
                return false;
            }

            // 按实际变换算 sigma，DPI 与 scale 动画下模糊强度与视觉尺寸一致。
            var matrix = canvas.TotalMatrix;
            var sigmaX = blurRadius * MathF.Sqrt(matrix.ScaleX * matrix.ScaleX + matrix.SkewY * matrix.SkewY);
            var sigmaY = blurRadius * MathF.Sqrt(matrix.ScaleY * matrix.ScaleY + matrix.SkewX * matrix.SkewX);

            // NOTE: 采样区外扩 3σ，否则 Clamp 会把边缘像素拉成条纹。
            var deviceRect = matrix.MapRect(rect);
            deviceRect.Inflate(3 * sigmaX, 3 * sigmaY);
            var deviceBounds = surface.Canvas.DeviceClipBounds;
            var capture = SKRectI.Intersect(deviceBounds, SKRectI.Round(deviceRect));
            if (capture.IsEmpty)
            {
                return false;
            }

            using var image = surface.Snapshot(capture);
            if (image is null)
            {
                return false;
            }

            using var blur = SKImageFilter.CreateBlur(sigmaX, sigmaY, SKShaderTileMode.Clamp);
            using var paint = new SKPaint { ImageFilter = blur, Color = SKColors.White.WithAlpha(alpha) };

            // 快照位于设备坐标，重置矩阵后原位贴回；圆角裁剪在重置前已设置，不受影响。
            canvas.ResetMatrix();
            canvas.DrawImage(image, capture.Left, capture.Top, SKSamplingOptions.Default, paint);
            canvas.SetMatrix(matrix);
            return true;
        }
    }
}
