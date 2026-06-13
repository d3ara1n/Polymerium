using System.Numerics;

namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     一个待渲染的四边形面：4 个模型空间顶点 + 4 个皮肤像素 UV，标记是否为外层。
/// </summary>
public readonly record struct SkinFace(
    Vector3 V0, Vector3 V1, Vector3 V2, Vector3 V3,
    Vector2 U0, Vector2 U1, Vector2 U2, Vector2 U3,
    bool Overlay);
