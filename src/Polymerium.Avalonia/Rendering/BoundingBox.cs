using System.Numerics;

namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     轴对齐包围盒，用于投影时缩放适配。
/// </summary>
public readonly record struct BoundingBox(Vector3 Min, Vector3 Max)
{
    public Vector3 Center => (Min + Max) * 0.5f;

    public Vector3[] Corners() =>
    [
        new(Min.X, Min.Y, Min.Z),
        new(Max.X, Min.Y, Min.Z),
        new(Min.X, Max.Y, Min.Z),
        new(Max.X, Max.Y, Min.Z),
        new(Min.X, Min.Y, Max.Z),
        new(Max.X, Min.Y, Max.Z),
        new(Min.X, Max.Y, Max.Z),
        new(Max.X, Max.Y, Max.Z)
    ];
}
