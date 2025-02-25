using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Huskui.Avalonia.Transitions;

public sealed class PageCoverOverTransition : PageTransitionBase
{
    public PageCoverOverTransition(TimeSpan? duration, DirectionFrom? direction) : base(duration) =>
        Direction = direction ?? DirectionFrom.Right;

    public PageCoverOverTransition() : this(null, null) { }

    public DirectionFrom Direction { get; set; }

    protected override void Cleanup(Visual? from, Visual? to)
    {
        base.Cleanup(from, to);

        if (from != null)
        {
            from.Opacity = 1;
            from.RenderTransform = null;
        }

        if (to != null)
        {
            to.Opacity = 1;
            to.RenderTransform = null;
        }
    }

    protected override void Configure(Builder from, Builder to, Lazy<Visual> parentAccessor)
    {
        var parent = parentAccessor.Value;
        var translateProperty = Direction switch
        {
            DirectionFrom.Right or DirectionFrom.Left => TranslateTransform.XProperty,
            DirectionFrom.Top or DirectionFrom.Bottom => TranslateTransform.YProperty,
            _ => throw new ArgumentOutOfRangeException()
        };
        var translateFrom = Direction switch
        {
            DirectionFrom.Left => -parent.Bounds.Width,
            DirectionFrom.Right => parent.Bounds.Width,
            DirectionFrom.Top => -parent.Bounds.Height,
            DirectionFrom.Bottom => parent.Bounds.Height,
            _ => throw new ArgumentOutOfRangeException()
        };

        from
           .Animation()
           .AddFrame(0d,
            [
                (ScaleTransform.ScaleXProperty, 1d), (ScaleTransform.ScaleYProperty, 1d), (Visual.OpacityProperty, 1d)
            ])
           .AddFrame(0.5d,
            [
                (ScaleTransform.ScaleXProperty, 0.98d),
                (ScaleTransform.ScaleYProperty, 0.98d),
                (Visual.OpacityProperty, 0d)
            ])
           .AddFrame(1d,
            [
                (ScaleTransform.ScaleXProperty, 0.98d),
                (ScaleTransform.ScaleYProperty, 0.98d),
                (Visual.OpacityProperty, 0d)
            ]);

        to
           .Animation(new BackEaseOut())
           .AddFrame(0.5d, [(ScaleTransform.ScaleXProperty, 0.98d), (ScaleTransform.ScaleYProperty, 0.98d)])
           .AddFrame(1d, [(ScaleTransform.ScaleXProperty, 1d), (ScaleTransform.ScaleYProperty, 1d)]);

        to
           .Animation(new CubicEaseOut())
           .AddFrame(0d, [(Visual.OpacityProperty, 0d), (translateProperty, translateFrom)])
           .AddFrame(1d, [(Visual.OpacityProperty, 1d), (translateProperty, 0d)]);
    }
}