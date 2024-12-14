﻿using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace Huskui.Avalonia.Transitions;

public sealed class PageCoverIn(TimeSpan? duration = null, DirectionFrom? direction = null)
    : PageTransitionBase(duration)
{
    public DirectionFrom Direction { get; set; } = direction ?? DirectionFrom.Right;

    protected override void Configure(Builder builder)
    {
        var opacityProperty = Visual.OpacityProperty;
        var scaleXProperty = ScaleTransform.ScaleXProperty;
        var scaleYProperty = ScaleTransform.ScaleYProperty;
        var parent = builder.Parent.Value;
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

        builder.AddFromAnimation([
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter
                    {
                        Property = scaleXProperty,
                        Value = 1d
                    },
                    new Setter
                    {
                        Property = scaleYProperty,
                        Value = 1d
                    },
                    new Setter
                    {
                        Property = opacityProperty,
                        Value = 1d
                    },
                    new Setter
                    {
                        Property = translateProperty,
                        Value = 0d
                    }
                }
            },
            new KeyFrame
            {
                Cue = new Cue(0.5d),
                Setters =
                {
                    new Setter
                    {
                        Property = scaleXProperty,
                        Value = 0.98d
                    },
                    new Setter
                    {
                        Property = scaleYProperty,
                        Value = 0.98d
                    },
                    new Setter
                    {
                        Property = opacityProperty,
                        Value = 0d
                    },
                    new Setter
                    {
                        Property = translateProperty,
                        Value = 0d
                    }
                }
            }
        ]);

        builder.AddToAnimation([
            new KeyFrame
            {
                Cue = new Cue(0.5d),
                Setters =
                {
                    new Setter
                    {
                        Property = scaleXProperty,
                        Value = 0.98d
                    },
                    new Setter
                    {
                        Property = scaleYProperty,
                        Value = 0.98d
                    }
                }
            },
            new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter
                    {
                        Property = scaleXProperty,
                        Value = 1d
                    },
                    new Setter
                    {
                        Property = scaleYProperty,
                        Value = 1d
                    }
                }
            }
        ], new BackEaseOut());

        builder.AddToAnimation([
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter
                    {
                        Property = opacityProperty,
                        Value = 0d
                    },
                    new Setter
                    {
                        Property = translateProperty,
                        Value = translateFrom
                    }
                }
            },
            new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter
                    {
                        Property = opacityProperty,
                        Value = 1d
                    },
                    new Setter
                    {
                        Property = translateProperty,
                        Value = 0d
                    }
                }
            }
        ], new CubicEaseOut());
    }
}