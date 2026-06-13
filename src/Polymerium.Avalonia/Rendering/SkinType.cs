namespace Polymerium.Avalonia.Rendering;

/// <summary>
///     Minecraft 皮肤格式，决定贴图布局、手臂宽度与是否含外层。
/// </summary>
public enum SkinType
{
    /// <summary>64×64 经典（Steve），宽手臂。</summary>
    Classic,

    /// <summary>64×64 纤细（Alex），窄手臂。</summary>
    Slim,

    /// <summary>64×32 旧版（1.7 及更早），无外层、左右肢体镜像复用右侧贴图。</summary>
    Legacy,
}
