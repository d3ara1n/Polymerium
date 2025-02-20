using Avalonia;

namespace Huskui.Avalonia.Transitions;

public sealed class PaintUpTransition(TimeSpan? duration = null) : PageTransitionBase(duration)
{
    protected override void Configure(Builder from, Builder to, Lazy<Visual> parentAccessor) => throw
        // 根本没法实现 Content.Background 和 Content 剥离，所有操作都必须应用在 ContentPresenter 上，局限性太高了
        new NotImplementedException();
}