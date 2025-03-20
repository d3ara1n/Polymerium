using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Huskui.Avalonia.Transitions;

public class PageSlideTransition(DirectionFrom direction = DirectionFrom.Right)
    : PageTransitionBase(TimeSpan.FromMilliseconds(297))
{
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
        var parent = parentAccessor.Value;
        var translateProperty = direction switch
        {
            DirectionFrom.Right or DirectionFrom.Left => TranslateTransform.XProperty,
            DirectionFrom.Top or DirectionFrom.Bottom => TranslateTransform.YProperty,
            _ => throw new ArgumentOutOfRangeException()
        };
        var translateFrom = direction switch
        {
            DirectionFrom.Left => -parent.Bounds.Width,
            DirectionFrom.Right => parent.Bounds.Width,
            DirectionFrom.Top => -parent.Bounds.Height,
            DirectionFrom.Bottom => parent.Bounds.Height,
            _ => throw new ArgumentOutOfRangeException()
        };

        from
           .Animation(new SplineEasing(0.1, 0.9, 0.2))
           .AddFrame(0d, [(translateProperty, 0d), (Visual.OpacityProperty, 1d)])
           .AddFrame(0.95d, [(Visual.OpacityProperty, 0d)])
           .AddFrame(1d, [(translateProperty, -translateFrom), (Visual.OpacityProperty, 0d)]);

        to
           .Animation(new SplineEasing(0.1, 0.9, 0.2))
           .AddFrame(0d, [(translateProperty, translateFrom), (Visual.OpacityProperty, 0d)])
           .AddFrame(0.05d, [(Visual.OpacityProperty, 1d)])
           .AddFrame(1d, [(translateProperty, 0d), (Visual.OpacityProperty, 1d)]);
    }
}