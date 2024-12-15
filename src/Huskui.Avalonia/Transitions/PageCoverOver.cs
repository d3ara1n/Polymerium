﻿using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Media;

namespace Huskui.Avalonia.Transitions;

public sealed class PageCoverOver : PageTransitionBase
{
    public PageCoverOver(TimeSpan? duration, DirectionFrom? direction) : base(duration)
    {
        Direction = direction ?? DirectionFrom.Right;
    }

    public PageCoverOver() : this(null, null)
    {
    }

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
        };

        from.Animation()
            .AddFrame(0d, [
                (ScaleTransform.ScaleXProperty, 1d),
                (ScaleTransform.ScaleYProperty, 1d),
                (Visual.OpacityProperty, 1d),
                (translateProperty, 0d)
            ]).AddFrame(0.5d, [
                (ScaleTransform.ScaleXProperty, 0.98d),
                (ScaleTransform.ScaleYProperty, 0.98d),
                (Visual.OpacityProperty, 0d),
                (translateProperty, 0d)
            ]);

        to.Animation(new BackEaseOut())
            .AddFrame(0.5d, [
                (ScaleTransform.ScaleXProperty, 0.98d),
                (ScaleTransform.ScaleYProperty, 0.98d)
            ]).AddFrame(1d, [
                (ScaleTransform.ScaleXProperty, 1d),
                (ScaleTransform.ScaleYProperty, 1d)
            ]);

        to.Animation(new CubicEaseOut())
            .AddFrame(0d, [
                (Visual.OpacityProperty, 0d),
                (translateProperty, translateFrom)
            ])
            .AddFrame(1d, [
                (Visual.OpacityProperty, 1d),
                (translateProperty, 0d)
            ]);
    }
}