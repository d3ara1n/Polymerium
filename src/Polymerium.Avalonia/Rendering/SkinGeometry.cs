using System.Collections.Generic;
using System.Numerics;

namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     Minecraft 玩家模型的几何与贴图 UV 定义，全部坐标已对齐 Mojang 1.8+ 规格。
/// </summary>
public static class SkinGeometry
{
    /// <summary>
    ///     标准 cube 顶点（±1），面序 <c>back, front, left, right, top, bottom</c>，每面 4 顶点。
    ///     顶点顺序与下方 UV 模板一一对应（已对照 Coloryr/MinecraftSkinRender 验证）。
    /// </summary>
    private static readonly float[] CubeVerts =
    [
        // back (z-)
        1, 1, -1, 1, -1, -1, -1, -1, -1, -1, 1, -1,
        // front (z+)
        -1, 1, 1, -1, -1, 1, 1, -1, 1, 1, 1, 1,
        // left (x-)
        -1, 1, -1, -1, -1, -1, -1, -1, 1, -1, 1, 1,
        // right (x+)
        1, 1, 1, 1, -1, 1, 1, -1, -1, 1, 1, -1,
        // top (y+)
        -1, 1, -1, -1, 1, 1, 1, 1, 1, 1, 1, -1,
        // bottom (y-)
        1, -1, -1, 1, -1, 1, -1, -1, 1, -1, -1, -1,
    ];

    // UV 模板（像素坐标，64×64 基准），面序与 CubeVerts 相同。
    private static readonly float[] HeadTex =
    [
        32, 8, 32, 16, 24, 16, 24, 8,
        8, 8, 8, 16, 16, 16, 16, 8,
        0, 8, 0, 16, 8, 16, 8, 8,
        16, 8, 16, 16, 24, 16, 24, 8,
        8, 0, 8, 8, 16, 8, 16, 0,
        24, 0, 24, 8, 16, 8, 16, 0,
    ];

    private static readonly float[] BodyTex =
    [
        24, 4, 24, 16, 16, 16, 16, 4,
        4, 4, 4, 16, 12, 16, 12, 4,
        0, 4, 0, 16, 4, 16, 4, 4,
        12, 4, 12, 16, 16, 16, 16, 4,
        4, 0, 4, 4, 12, 4, 12, 0,
        20, 0, 20, 4, 12, 4, 12, 0,
    ];

    private static readonly float[] LegArmTex =
    [
        12, 4, 12, 16, 16, 16, 16, 4,
        4, 4, 4, 16, 8, 16, 8, 4,
        0, 4, 0, 16, 4, 16, 4, 4,
        8, 4, 8, 16, 12, 16, 12, 4,
        4, 0, 4, 4, 8, 4, 8, 0,
        12, 0, 12, 4, 8, 4, 8, 0,
    ];

    private static readonly float[] SlimArmTex =
    [
        11, 4, 11, 16, 14, 16, 14, 4,
        4, 4, 4, 16, 7, 16, 7, 4,
        0, 4, 0, 16, 4, 16, 4, 4,
        7, 4, 7, 16, 11, 16, 11, 4,
        4, 0, 4, 4, 7, 4, 7, 0,
        10, 0, 10, 4, 7, 4, 7, 0,
    ];

    /// <summary>外层相对本体的放大系数（1/8）。</summary>
    private const float OverlayScale = 1.125f;

    private readonly record struct PartSpec(
        float Hx, float Hy, float Hz, Vector3 Pivot,
        float[] Tex, float BaseU, float BaseV, float OverU, float OverV, bool HasOverlay);

    /// <summary>
    ///     构建头像网格（仅 head，本体 + 外层），box 中心置于原点便于单独投影。
    /// </summary>
    public static List<SkinFace> BuildHead()
    {
        var head = new PartSpec(0.5f, 0.5f, 0.5f, Vector3.Zero, HeadTex, 0, 0, 32, 0, true);
        var faces = new List<SkinFace>();
        AddPart(faces, head, false);
        // 头部外层（hat）在所有格式中都存在。
        AddPart(faces, head, true);
        return faces;
    }

    /// <summary>
    ///     构建全身网格（head + body + 双臂 + 双腿，本体与外层）。
    /// </summary>
    public static List<SkinFace> BuildBody(SkinType type)
    {
        var parts = type == SkinType.Legacy ? LegacyParts() : NewParts(type);
        var faces = new List<SkinFace>();
        foreach (var p in parts)
            AddPart(faces, p, false);
        foreach (var p in parts)
            if (p.HasOverlay)
                AddPart(faces, p, true);
        return faces;
    }

    private static IEnumerable<PartSpec> NewParts(SkinType type)
    {
        var armTex = type == SkinType.Slim ? SlimArmTex : LegArmTex;
        var armHx = type == SkinType.Slim ? 0.1875f : 0.25f;
        var armPx = type == SkinType.Slim ? 0.6875f : 0.75f;
        yield return new(0.5f, 0.5f, 0.5f, new(0, 1.25f, 0), HeadTex, 0, 0, 32, 0, true);
        yield return new(0.5f, 0.75f, 0.25f, Vector3.Zero, BodyTex, 16, 16, 16, 32, true);
        yield return new(armHx, 0.75f, 0.25f, new(armPx, 0, 0), armTex, 32, 48, 48, 48, true);
        yield return new(armHx, 0.75f, 0.25f, new(-armPx, 0, 0), armTex, 40, 16, 40, 32, true);
        yield return new(0.25f, 0.75f, 0.25f, new(0.25f, -1.5f, 0), LegArmTex, 0, 16, 0, 48, true);
        yield return new(0.25f, 0.75f, 0.25f, new(-0.25f, -1.5f, 0), LegArmTex, 16, 48, 0, 32, true);
    }

    private static IEnumerable<PartSpec> LegacyParts()
    {
        // 旧版无外层（head 除外），左右肢体镜像复用右侧贴图区。
        yield return new(0.5f, 0.5f, 0.5f, new(0, 1.25f, 0), HeadTex, 0, 0, 32, 0, true);
        yield return new(0.5f, 0.75f, 0.25f, Vector3.Zero, BodyTex, 16, 16, 0, 0, false);
        yield return new(0.25f, 0.75f, 0.25f, new(0.75f, 0, 0), LegArmTex, 40, 16, 0, 0, false);
        yield return new(0.25f, 0.75f, 0.25f, new(-0.75f, 0, 0), LegArmTex, 40, 16, 0, 0, false);
        yield return new(0.25f, 0.75f, 0.25f, new(0.25f, -1.5f, 0), LegArmTex, 0, 16, 0, 0, false);
        yield return new(0.25f, 0.75f, 0.25f, new(-0.25f, -1.5f, 0), LegArmTex, 0, 16, 0, 0, false);
    }

    private static void AddPart(ICollection<SkinFace> faces, PartSpec p, bool overlay)
    {
        var hx = p.Hx * (overlay ? OverlayScale : 1f);
        var hy = p.Hy * (overlay ? OverlayScale : 1f);
        var hz = p.Hz * (overlay ? OverlayScale : 1f);
        var offU = overlay ? p.OverU : p.BaseU;
        var offV = overlay ? p.OverV : p.BaseV;

        for (var f = 0; f < 6; f++)
        {
            var vs = new Vector3[4];
            var uvs = new Vector2[4];
            for (var c = 0; c < 4; c++)
            {
                var vi = f * 12 + c * 3;
                vs[c] = new Vector3(
                        CubeVerts[vi] * hx,
                        CubeVerts[vi + 1] * hy,
                        CubeVerts[vi + 2] * hz)
                    + p.Pivot;
                var ui = f * 8 + c * 2;
                uvs[c] = new(p.Tex[ui] + offU, p.Tex[ui + 1] + offV);
            }

            faces.Add(new(vs[0], vs[1], vs[2], vs[3], uvs[0], uvs[1], uvs[2], uvs[3], overlay));
        }
    }

    /// <summary>
    ///     计算网格的轴对齐包围盒（含外层放大）。
    /// </summary>
    public static BoundingBox ComputeBounds(IEnumerable<SkinFace> faces)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var f in faces)
            foreach (var v in new[] { f.V0, f.V1, f.V2, f.V3 })
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

        return new(min, max);
    }
}
