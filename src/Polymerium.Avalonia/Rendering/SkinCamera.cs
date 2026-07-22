using System;
using System.Numerics;
using SkiaSharp;

namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     等距投影相机：绕 Y、X 旋转模型后正交投影到画布，自动缩放使模型居中适配。
/// </summary>
public sealed class SkinCamera
{
    /// <summary>模型在画布中的垂直对齐方式。</summary>
    public enum VerticalAlign
    {
        /// <summary>垂直居中（默认），全身视图用。</summary>
        Center,

        /// <summary>头顶贴画布顶边，配合画布截断用于半身像（Cover）。</summary>
        Top
    }

    private const float Deg = MathF.PI / 180f;

    /// <summary>绕 Y 轴旋转角度（度）。0° 看到正面，正值顺时针。</summary>
    public float YawDeg { get; set; }

    /// <summary>绕 X 轴旋转角度（度）。正值从上往下俯视，负值从下往上仰视。</summary>
    public float PitchDeg { get; set; }

    /// <summary>输出画布宽。</summary>
    public int Width { get; set; }

    /// <summary>输出画布高。</summary>
    public int Height { get; set; }

    /// <summary>模型相对画布的缩放系数（0~1，1.0 = 填满贴边无留白）。</summary>
    public float FitScale { get; set; } = 1f;

    /// <summary>模型在画布中的垂直对齐方式，影响 <see cref="Fit" /> 的垂直平移。</summary>
    public VerticalAlign Alignment { get; set; } = VerticalAlign.Center;

    /// <summary>视图矩阵：先绕 Y 再绕 X 旋转（row-vector 约定下点先经 Y 再经 X）。</summary>
    public Matrix4x4 BuildView() => Matrix4x4.CreateRotationY(YawDeg * Deg) * Matrix4x4.CreateRotationX(PitchDeg * Deg);

    /// <summary>
    ///     旋转包围盒后计算等比缩放与平移，使模型居中并翻转 Y（屏幕 Y 向下）。
    /// </summary>
    public (float Scale, Vector2 Translate) Fit(BoundingBox box, Matrix4x4 view)
    {
        float minx = float.MaxValue, maxx = float.MinValue;
        float miny = float.MaxValue, maxy = float.MinValue;
        foreach (var c in box.Corners())
        {
            var r = Vector3.Transform(c, view);
            minx = MathF.Min(minx, r.X);
            maxx = MathF.Max(maxx, r.X);
            miny = MathF.Min(miny, r.Y);
            maxy = MathF.Max(maxy, r.Y);
        }

        var sizeX = maxx - minx;
        var sizeY = maxy - miny;
        var sx = sizeX > 0 ? Width / sizeX : 1f;
        var sy = sizeY > 0 ? Height / sizeY : 1f;
        var scale = MathF.Min(sx, sy) * FitScale;

        var cx = (minx + maxx) * 0.5f;
        var cy = (miny + maxy) * 0.5f;
        var tx = Width * 0.5f - cx * scale;
        // 屏幕坐标 Y 向下：screenY = trY - rY*scale。
        // Center：模型中心(cy)对齐画布中心；Top：旋转后最高点(maxy)贴画布顶边(0)。
        var ty = Alignment == VerticalAlign.Top ? maxy * scale : Height * 0.5f + cy * scale;
        return (scale, new(tx, ty));
    }

    /// <summary>
    ///     把模型顶点投影到屏幕坐标，并返回旋转后的 Z（用于深度排序）。
    /// </summary>
    public (SKPoint Point, float Depth) Project(Vector3 v, Matrix4x4 view, float scale, Vector2 tr)
    {
        var r = Vector3.Transform(v, view);
        var pt = new SKPoint(r.X * scale + tr.X, tr.Y - r.Y * scale);
        return (pt, r.Z);
    }
}
