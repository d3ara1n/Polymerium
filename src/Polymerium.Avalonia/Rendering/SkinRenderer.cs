using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SkiaSharp;

namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     纯软件 Minecraft 皮肤 3D 渲染器：输入皮肤 PNG，输出头像与全身预览。
///     不依赖任何外部网络渲染服务。
/// </summary>
public sealed class SkinRenderer
{
    /// <summary>
    ///     渲染 3D 头像（默认正面对着镜头，保留立体厚度）。
    /// </summary>
    /// <param name="skin">皮肤位图（64×64 或 64×32）。</param>
    /// <param name="yawDeg">绕 Y 轴旋转角度，默认 0°（正面对着镜头）。</param>
    /// <param name="pitchDeg">绕 X 轴俯视角度，默认 0°（平视）。</param>
    /// <param name="size">输出边长（正方形）。</param>
    public SKImage RenderHead(SKBitmap skin, float yawDeg = 0f, float pitchDeg = 0f, int size = 256)
    {
        var faces = SkinGeometry.BuildHead();
        return Draw(skin, faces, yawDeg, pitchDeg, size, size);
    }

    /// <summary>
    ///     渲染 3D 全身（默认等距右上俯视）。
    /// </summary>
    /// <param name="skin">皮肤位图。</param>
    /// <param name="yawDeg">绕 Y 轴旋转角度，默认 45°（正面 + 右侧）。</param>
    /// <param name="pitchDeg">绕 X 轴俯视角度，正值从上往下（俯视），默认 15°。</param>
    /// <param name="width">输出宽。</param>
    /// <param name="height">输出高。</param>
    public SKImage RenderBody(
        SKBitmap skin,
        float yawDeg = 45f,
        float pitchDeg = 15f,
        int width = 210,
        int height = 420)
    {
        var type = SkinFormat.Detect(skin);
        var faces = SkinGeometry.BuildBody(type);
        return Draw(skin, faces, yawDeg, pitchDeg, width, height);
    }

    /// <summary>
    ///     渲染四方向全身正视（Front → Right → Back → Left，pitch=0），用于旋转预览。
    /// </summary>
    public IReadOnlyList<SKImage> RenderBodyViews(SKBitmap skin, int width = 210, int height = 420)
    {
        var result = new List<SKImage>(4);
        foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
        {
            result.Add(RenderBody(skin, yaw, 0f, width, height));
        }

        return result;
    }

    /// <summary>
    ///     按视图类型调度渲染：<see cref="SkinViewType.Body" /> 与 <see cref="SkinViewType.Cover" />
    ///     为等距侧身（右上俯视），Cover 经画布裁切为上半身特写；其余为 pitch=0 的 3D 正视图（保留立体厚度，不倾斜）。
    /// </summary>
    public SKImage Render(SKBitmap skin, SkinViewType view) =>
        view switch
        {
            SkinViewType.Face => RenderHead(skin),
            SkinViewType.Body => RenderBody(skin),
            SkinViewType.Cover => RenderCover(skin),
            SkinViewType.Front => RenderBody(skin, 0f, 0f),
            SkinViewType.Right => RenderBody(skin, 90f, 0f),
            SkinViewType.Back => RenderBody(skin, 180f, 0f),
            SkinViewType.Left => RenderBody(skin, 270f, 0f),
            _ => throw new ArgumentOutOfRangeException(nameof(view))
        };

    /// <summary>
    ///     渲染半身像（Cover）：用全身几何与 <see cref="SkinViewType.Body" /> 相同的缩放，
    ///     头顶贴画布顶边，画布只截取上半身，腿部超出底边自然截断——视觉上如照片框住上半身。
    /// </summary>
    private SKImage RenderCover(SKBitmap skin)
    {
        var type = SkinFormat.Detect(skin);
        var faces = SkinGeometry.BuildBody(type);
        const int w = 210, h = 420;
        // 上半身理想比例为 0.625(头+躯干 2.5 / 全身 4.0)；在此基础上再收 15%，
        // 截断点上移到大腿根部稍上方，少露腿，更像框住上半身的证件照。
        var crop = new SKRectI(0, 0, w, (int)MathF.Round(h * 0.53f));
        return Draw(skin, faces, 45f, 15f, w, h, SkinCamera.VerticalAlign.Top, crop);
    }

    private static SKImage Draw(
        SKBitmap skin,
        IList<SkinFace> faces,
        float yaw,
        float pitch,
        int w,
        int h,
        SkinCamera.VerticalAlign valign = SkinCamera.VerticalAlign.Center,
        SKRectI? crop = null)
    {
        var cam = new SkinCamera { YawDeg = yaw, PitchDeg = pitch, Width = w, Height = h, Alignment = valign };
        var view = cam.BuildView();
        var (scale, tr) = cam.Fit(SkinGeometry.ComputeBounds(faces), view);

        using var surface = SKSurface.Create(new SKImageInfo(w, h));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        // 投影 + 背面剔除
        var visible = new List<ProjectedFace>(faces.Count);
        foreach (var f in faces)
        {
            var pf = ProjectFace(f, view, scale, tr);
            if (SignedArea(pf.Pts) < 0f)
            {
                visible.Add(pf);
            }
        }

        // 本体先画（远→近），外层后画（远→近，叠加在本体之上）。
        // Nearest 最近邻采样保留 Minecraft 像素艺术的锐利边缘；采样选项随 shader 而非 paint 传递。
        using var shader = skin.ToShader(SKShaderTileMode.Clamp,
                                         SKShaderTileMode.Clamp,
                                         new SKSamplingOptions(SKFilterMode.Nearest));
        using var paint = new SKPaint { IsAntialias = true, Shader = shader };

        foreach (var pf in visible.Where(p => !p.Overlay).OrderBy(p => p.Depth))
        {
            DrawFace(canvas, skin, pf.Face, pf.Pts, paint);
        }

        foreach (var pf in visible.Where(p => p.Overlay).OrderBy(p => p.Depth))
        {
            DrawFace(canvas, skin, pf.Face, pf.Pts, paint);
        }

        return crop is { } rect ? surface.Snapshot(rect) : surface.Snapshot();
    }

    private static ProjectedFace ProjectFace(SkinFace f, Matrix4x4 view, float scale, Vector2 tr)
    {
        var pts = new SKPoint[4];
        var vs = new[] { f.V0, f.V1, f.V2, f.V3 };
        var depth = 0f;
        for (var i = 0; i < 4; i++)
        {
            var r = Vector3.Transform(vs[i], view);
            depth += r.Z;
            pts[i] = new(r.X * scale + tr.X, tr.Y - r.Y * scale);
        }

        return new(pts, depth * 0.25f, f.Overlay, f);
    }

    private static void DrawFace(SKCanvas canvas, SKBitmap skin, SkinFace f, SKPoint[] pts, SKPaint paint)
    {
        // SKShader.CreateBitmap 的纹理坐标是像素坐标（直接索引 bitmap），无需归一化。
        var uvs = new[]
        {
            new SKPoint(f.U0.X, f.U0.Y),
            new SKPoint(f.U1.X, f.U1.Y),
            new SKPoint(f.U2.X, f.U2.Y),
            new SKPoint(f.U3.X, f.U3.Y)
        };
        using var verts = SKVertices.CreateCopy(SKVertexMode.TriangleFan, pts, uvs, null);
        canvas.DrawVertices(verts, SKBlendMode.SrcOver, paint);
    }

    // 屏幕空间有符号面积（Skia Y 向下）：< 0 视为正面朝相机（与 CubeModel 顶点序匹配）。
    private static float SignedArea(SKPoint[] p)
    {
        var a = 0f;
        for (var i = 0; i < 4; i++)
        {
            var j = (i + 1) % 4;
            a += p[i].X * p[j].Y - p[j].X * p[i].Y;
        }

        return a * 0.5f;
    }

    private sealed record ProjectedFace(SKPoint[] Pts, float Depth, bool Overlay, SkinFace Face);
}
