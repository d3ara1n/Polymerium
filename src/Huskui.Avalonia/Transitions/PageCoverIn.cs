using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace Huskui.Avalonia.Transitions;

public class PageCoverIn(TimeSpan? duration = null, DirectionFrom? direction = null) : IPageTransition
{
    public TimeSpan Duration { get; set; } = duration ?? TimeSpan.FromMilliseconds(197);
    public DirectionFrom Direction { get; set; } = direction ?? DirectionFrom.Right;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var tasks = new List<Task>();
        var opacityProperty = Visual.OpacityProperty;
        var scaleXProperty = ScaleTransform.ScaleXProperty;
        var scaleYProperty = ScaleTransform.ScaleYProperty;

        var parent = this.GetVisualParent(from, to);
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

        if (!forward) (from, to) = (to, from);

        if (from != null)
        {
            // from 需要添加 translate 动画将上次位移重置到 0
            var animation = new Animation
            {
                FillMode = FillMode.Forward,
                PlaybackDirection = forward ? PlaybackDirection.Normal : PlaybackDirection.Reverse,
                Duration = Duration,
                Children =
                {
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
                }
            };
            tasks.Add(animation.RunAsync(from, cancellationToken));
        }

        if (to != null)
        {
            to.IsVisible = true;
            var easingAnimation = new Animation
            {
                FillMode = FillMode.Forward,
                PlaybackDirection = forward ? PlaybackDirection.Normal : PlaybackDirection.Reverse,
                Easing = new BackEaseOut(),
                Duration = Duration,
                Children =
                {
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
                }
            };
            var animation = new Animation
            {
                FillMode = FillMode.Forward,
                PlaybackDirection = forward ? PlaybackDirection.Normal : PlaybackDirection.Reverse,
                Duration = Duration,
                Easing = new CubicEaseOut(),
                Children =
                {
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
                }
            };
            tasks.Add(easingAnimation.RunAsync(to, cancellationToken));
            tasks.Add(animation.RunAsync(to, cancellationToken));
        }

        await Task.WhenAll(tasks);


        // TransitioningContentControl 的动画是直接操作的 ContentPresenter，而且 Avalonia 的动画在播放完后不会重置属性，所以这里得手动重置避免留下后遗症
        if (from != null && !cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = !forward;
            from.Opacity = 1.0d;
            from.RenderTransform = null;
        }

        if (to != null && !cancellationToken.IsCancellationRequested)
        {
            to.IsVisible = forward;
            to.Opacity = 1.0d;
            to.RenderTransform = null;
        }
    }
}