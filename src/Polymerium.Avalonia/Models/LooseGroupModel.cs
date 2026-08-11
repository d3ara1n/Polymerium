namespace Polymerium.Avalonia.Models;

// NOTE: 散装包（Source == null）的虚拟归组，共享单例——不渲染组头与导引线，永远沉底。
public sealed class LooseGroupModel : GroupModel
{
    public override bool RequireGuideLine => false;
}
