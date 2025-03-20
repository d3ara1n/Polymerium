using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Huskui.Avalonia.Transitions;

public sealed class PopUpTransition : PageTransitionBase
{
    public PopUpTransition() { }

    public PopUpTransition(TimeSpan? duration = null) : base(duration) { }

    protected override void Cleanup(Visual? from, Visual? to)
    {
        base.Cleanup(from, to);
        
        if (from != null)
            from.RenderTransform = null;
        if (to != null)
            to.RenderTransform = null;
    }

    protected override void Configure(Builder from, Builder to, Lazy<Visual> parentAccessor)
    {
        from
           .Animation(new SineEaseIn())
           .AddFrame(0d, [(Visual.OpacityProperty, 1d)])
           .AddFrame(1d, [(Visual.OpacityProperty, 0d)]);

        to
           .Animation(new BackEaseOut())
           .AddFrame(0d, [(ScaleTransform.ScaleXProperty, 0.98d), (ScaleTransform.ScaleYProperty, 0.98d)])
           .AddFrame(1d, [(ScaleTransform.ScaleXProperty, 1d), (ScaleTransform.ScaleYProperty, 1d)]);

        to
           .Animation(new SineEaseOut())
           .AddFrame(0d, [(Visual.OpacityProperty, 0d)])
           .AddFrame(1d, [(Visual.OpacityProperty, 1d)]);
    }
}