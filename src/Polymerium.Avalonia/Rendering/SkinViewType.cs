namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     皮肤渲染视图类型，对应 poly://skin?type= 的取值。
///     <see cref="Body" /> 与 <see cref="Cover" /> 为等距侧身（右上俯视），其中 Body 含双腿、Cover 截断双腿为上半身特写；
///     其余均为 pitch=0 的 3D 正视图（保留立体厚度，不倾斜）。
/// </summary>
public enum SkinViewType
{
    Face,
    Body,
    Cover,
    Front,
    Right,
    Back,
    Left,
}
