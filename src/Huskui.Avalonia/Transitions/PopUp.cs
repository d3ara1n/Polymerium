using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace Huskui.Avalonia.Transitions;

public sealed class PopUp(TimeSpan? duration = null) : PageTransitionBase(duration)
{
    protected override void Configure(Builder builder)
    {
        var scaleXProperty = ScaleTransform.ScaleXProperty;
        var scaleYProperty = ScaleTransform.ScaleYProperty;
        var opacityProperty = Visual.OpacityProperty;

        builder.AddFromAnimation([
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter
                    {
                        Property = opacityProperty,
                        Value = 1.0d
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
                        Property = opacityProperty, Value = 0d
                    }
                }
            }
        ]);

        builder.AddToAnimation([
            new KeyFrame
            {
                Cue = new Cue(0d),
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
                    }
                }
            }
        ]);
    }
}