using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Huskui.Avalonia.Transitions;

public class FocusOnTransition : PageTransitionBase
{
    public FocusOnTransition() : this(null, null)
    {
    }

    public FocusOnTransition(TimeSpan? duration, DirectionFrom? direction) : base(duration) =>
        Direction = direction ?? DirectionFrom.Bottom;

    public DirectionFrom Direction { get; set; }

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
        } / 4;

        from
            .Animation()
            .AddFrame(0d, [(Visual.OpacityProperty, 1d)])
            .AddFrame(1d, [(Visual.OpacityProperty, 0d)]);

        to
            .Animation(new SineEaseOut())
            .AddFrame(0d, [
                (ScaleTransform.ScaleXProperty, 1.1d),
                (ScaleTransform.ScaleYProperty, 1.1d),
                (translateProperty, translateFrom)
            ])
            .AddFrame(1d, [
                (ScaleTransform.ScaleXProperty, 1d),
                (ScaleTransform.ScaleYProperty, 1d),
                (translateProperty, 0)
            ]);

        to.Animation()
            .AddFrame(0d, [(Visual.OpacityProperty, 0d)])
            .AddFrame(1d, [(Visual.OpacityProperty, 1d)]);
    }
}