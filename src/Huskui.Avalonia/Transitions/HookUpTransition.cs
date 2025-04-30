using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Huskui.Avalonia.Transitions;

public class HookUpTransition : PageTransitionBase
{
    protected override void Configure(Builder from, Builder to, Lazy<Visual> parentAccessor)
    {
        var height = 72d;
        from
           .Animation(new CubicEaseOut())
           .AddFrame(0d, [(TranslateTransform.YProperty, 0), (Visual.OpacityProperty, 1d)])
           .AddFrame(1d, [(TranslateTransform.YProperty, height), (Visual.OpacityProperty, 0d)]);

        to
           .Animation(new CubicEaseOut())
           .AddFrame(0d, [(TranslateTransform.YProperty, height), (Visual.OpacityProperty, 0d)])
           .AddFrame(1d, [(TranslateTransform.YProperty, 0), (Visual.OpacityProperty, 1d)]);
    }
}