using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Huskui.Avalonia.Transitions;

public abstract class PageTransitionBase(TimeSpan? duration = null) : IPageTransition
{
    private static readonly TimeSpan _defaultDuration = TimeSpan.FromMilliseconds(197);
    private static readonly Easing _defaultEasing = new LinearEasing();

    public TimeSpan Duration { get; set; } = duration ?? _defaultDuration;

    public class Builder
    {
        private readonly IList<AnimationBuilder> _builder = new List<AnimationBuilder>();
        private readonly TimeSpan _duration;

        public Builder(TimeSpan duration)
        {
            _duration = duration;
        }

        public class AnimationBuilder
        {
            private TimeSpan? _duration;
            private FillMode? _fillMode;
            private double? _speedRatio;
            private Easing? _easing;
            private TimeSpan? _delay;
            private TimeSpan? _gap;

            private readonly IList<FrameBuilder> _frames = new List<FrameBuilder>();

            public AnimationBuilder(TimeSpan? duration = null, Easing? easing = null)
            {
                _duration = duration;
                _easing = easing;
            }

            public class FrameBuilder
            {
                private double? _cue;
                private TimeSpan? _keyTime;
                private KeySpline? _keySpline;

                private readonly IList<(AvaloniaProperty Property, object? Value)> _setters =
                    new List<(AvaloniaProperty, object?)>();

                public FrameBuilder(double? cue, TimeSpan? keyTime)
                {
                    _cue = cue;
                    _keyTime = keyTime;
                }

                internal KeyFrame Build()
                {
                    var frame = new KeyFrame();
                    if (_cue.HasValue) frame.Cue = new Cue(_cue.Value);
                    if (_keyTime.HasValue) frame.KeyTime = _keyTime.Value;
                    if (_keySpline != null) frame.KeySpline = _keySpline;

                    foreach (var setter in _setters) frame.Setters.Add(new Setter(setter.Property, setter.Value));

                    return frame;
                }

                public FrameBuilder WithSetter(AvaloniaProperty property, object? value)
                {
                    _setters.Add((property, value));
                    return this;
                }

                public FrameBuilder WithKeyTime(TimeSpan keyTime)
                {
                    _keyTime = keyTime;
                    return this;
                }

                public FrameBuilder WithCue(double cue)
                {
                    _cue = cue;
                    return this;
                }
            }

            public AnimationBuilder AddFrame(double cue, Action<FrameBuilder> builder)
            {
                var frame = new FrameBuilder(cue, null);
                _frames.Add(frame);
                return this;
            }

            public AnimationBuilder AddFrame(TimeSpan keyTime, Action<FrameBuilder> builder)
            {
                var frame = new FrameBuilder(null, keyTime);
                _frames.Add(frame);
                return this;
            }

            public AnimationBuilder AddFrame(double cue,
                IEnumerable<(AvaloniaProperty Property, object? Value)> setters)
            {
                var frame = new FrameBuilder(cue, null);
                foreach (var setter in setters) frame.WithSetter(setter.Property, setter.Value);

                _frames.Add(frame);
                return this;
            }

            public AnimationBuilder AddFrame(TimeSpan keyTime,
                IEnumerable<(AvaloniaProperty Property, object? Value)> setters)
            {
                var frame = new FrameBuilder(null, keyTime);
                foreach (var setter in setters) frame.WithSetter(setter.Property, setter.Value);

                _frames.Add(frame);
                return this;
            }

            public AnimationBuilder WithEasing(Easing easing)
            {
                _easing = easing;
                return this;
            }

            public AnimationBuilder WithFillMode(FillMode fillMode)
            {
                _fillMode = fillMode;
                return this;
            }

            public AnimationBuilder WithSpeedRatio(double speedRatio)
            {
                _speedRatio = speedRatio;
                return this;
            }

            public AnimationBuilder WithDuration(TimeSpan duration)
            {
                _duration = duration;
                return this;
            }

            public AnimationBuilder WithDelay(TimeSpan delay)
            {
                _delay = delay;
                return this;
            }

            public AnimationBuilder WithGap(TimeSpan gap)
            {
                _gap = gap;
                return this;
            }

            internal Animation Build(bool forward, TimeSpan duration, TimeSpan delay, TimeSpan gap, FillMode fillMode,
                double speedRatio,
                Easing easing)
            {
                var animation = new Animation
                {
                    Duration = _duration ?? duration,
                    FillMode = _fillMode ?? fillMode,
                    SpeedRatio = _speedRatio ?? speedRatio,
                    Easing = _easing ?? easing,
                    Delay = _delay ?? delay,
                    DelayBetweenIterations = _gap ?? gap,
                    PlaybackDirection = forward ? PlaybackDirection.Normal : PlaybackDirection.Reverse
                };
                foreach (var sub in _frames)
                {
                    var frame = sub.Build();
                    animation.Children.Add(frame);
                }

                return animation;
            }
        }

        internal IEnumerable<Animation> Build(bool forward)
        {
            return _builder.Select(x =>
                x.Build(forward, _duration, TimeSpan.Zero, TimeSpan.Zero, FillMode.Forward, 1.0d, new LinearEasing()));
        }

        public AnimationBuilder Animation(TimeSpan? duration = null, Easing? easing = null)
        {
            var animation = new AnimationBuilder(duration, easing);
            _builder.Add(animation);
            return animation;
        }

        public AnimationBuilder Animation(TimeSpan duration)
        {
            return Animation(duration, null);
        }

        public AnimationBuilder Animation(Easing easing)
        {
            return Animation(null, easing);
        }

        public AnimationBuilder Animation()
        {
            return Animation(null, null);
        }
    }

    protected virtual void Setup(Visual? from, Visual? to)
    {
        // from 看不看得见无所谓

        if (to != null) to.IsVisible = true;
    }

    protected virtual void Cleanup(Visual? from, Visual? to)
    {
        if (from != null)
        {
            from.IsVisible = false;
            from.Opacity = 1.0d;
            from.RenderTransform = null;
        }

        if (to != null)
        {
            to.IsVisible = true;
            to.Opacity = 1.0d;
            to.RenderTransform = null;
        }
    }

    /// <summary>
    ///     Gets the common visual parent of the two control.
    /// </summary>
    /// <param name="from">The from control.</param>
    /// <param name="to">The to control.</param>
    /// <returns>The common parent.</returns>
    /// <exception cref="ArgumentException">
    ///     The two controls do not share a common parent.
    /// </exception>
    /// <remarks>
    ///     Any one of the parameters may be null, but not both.
    /// </remarks>
    public Visual GetVisualParent(Visual? from, Visual? to)
    {
        var p1 = (from ?? to)!.GetVisualParent();
        var p2 = (to ?? from)!.GetVisualParent();

        if (p1 != null && p2 != null && p1 != p2)
            throw new ArgumentException(
                $"Controls for {GetType().Name} must have same parent.");

        return p1 ?? throw new InvalidOperationException(
            "Cannot determine visual parent.");
    }


    protected abstract void Configure(Builder from, Builder to, Lazy<Visual> parentAccessor);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var tasks = new List<Task>();

        // 在反向时动画会反正施加到 from 和 to 上，to 会倒着播放原先消失的动画，这里取巧直接反转 from 和 to，并倒转双方动画实现
        if (!forward) (from, to) = (to, from);

        Setup(from, to);

        // 捕获交换过的 from, to，不过顺序不影响
        var fromBuilder = new Builder(Duration);
        var toBuilder = new Builder(Duration);
        Configure(fromBuilder, toBuilder, new Lazy<Visual>(() => GetVisualParent(from, to)));

        var (fromAnimations, toAnimations) = (fromBuilder.Build(forward), toBuilder.Build(forward));

        if (from != null)
            foreach (var animation in fromAnimations)
                tasks.Add(animation.RunAsync(from, cancellationToken));

        if (to != null)
            foreach (var animation in toAnimations)
                tasks.Add(animation.RunAsync(to, cancellationToken));

        await Task.WhenAll(tasks);

        // TransitioningContentControl 的动画直接作用在两个 ContentPresenter 上，Avalonia 的 Animation 也不会自动复原属性，需要手动重置以免留下副作用
        if (!cancellationToken.IsCancellationRequested) Cleanup(forward ? from : to, forward ? to : from);
    }
}