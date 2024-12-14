using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.VisualTree;

namespace Huskui.Avalonia.Transitions;

public abstract class PageTransitionBase(TimeSpan? duration = null) : IPageTransition
{
    private static readonly TimeSpan _defaultDuration = TimeSpan.FromMilliseconds(197);
    private static readonly Easing _defaultEasing = new LinearEasing();

    public TimeSpan Duration { get; set; } = duration ?? _defaultDuration;

    public sealed class Builder(Lazy<Visual> parent)
    {
        public record AnimationGroup(
            IEnumerable<KeyFrame> Frames,
            Easing? EasingFunction = null,
            FillMode? FillMode = null,
            double? SpeedRatio = null);

        private readonly IList<AnimationGroup> _from = new List<AnimationGroup>();
        private readonly IList<AnimationGroup> _to = new List<AnimationGroup>();

        public Lazy<Visual> Parent => parent;

        public Builder AddFromAnimation(IEnumerable<KeyFrame> frames, Easing? easingFunction = null,
            FillMode? fillMode = null, double? speedRatio = null)
        {
            _from.Add(new AnimationGroup(frames, easingFunction, fillMode, speedRatio));
            return this;
        }

        public Builder AddToAnimation(IEnumerable<KeyFrame> frames, Easing? easingFunction = null,
            FillMode? fillMode = null, double? speedRatio = null)
        {
            _to.Add(new AnimationGroup(frames, easingFunction, fillMode, speedRatio));
            return this;
        }

        internal (IEnumerable<AnimationGroup>, IEnumerable<AnimationGroup>) Build()
        {
            return (_from, _to);
        }
    }

    protected abstract void Configure(Builder builder);

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        var tasks = new List<Task>();

        // 在反向时动画会反正施加到 from 和 to 上，to 会倒着播放原先消失的动画，这里取巧直接反转 from 和 to，并倒转双方动画实现
        if (!forward) (from, to) = (to, from);

        // 捕获交换过的 from, to，不过顺序不影响
        var builder = new Builder(new Lazy<Visual>(() => GetVisualParent(from, to)));
        Configure(builder);

        var (fromAnimations, toAnimations) = builder.Build();

        if (from != null)
            foreach (var group in fromAnimations)
            {
                var animation = FromAnimationGroup(group, forward);
                tasks.Add(animation.RunAsync(from, cancellationToken));
            }

        if (to != null)
            foreach (var group in toAnimations)
            {
                var animation = FromAnimationGroup(group, forward);
                tasks.Add(animation.RunAsync(to, cancellationToken));
            }

        await Task.WhenAll(tasks);

        // TransitioningContentControl 的动画直接作用在两个 ContentPresenter 上，Avalonia 的 Animation 也不会自动复原属性，需要手动重置以免留下副作用
        if (!cancellationToken.IsCancellationRequested) Cleanup(forward ? from : to, forward ? to : from);
    }

    private Animation FromAnimationGroup(Builder.AnimationGroup group, bool forward)
    {
        var animation = new Animation
        {
            FillMode = group.FillMode ?? FillMode.Forward,
            PlaybackDirection = forward ? PlaybackDirection.Normal : PlaybackDirection.Reverse,
            Duration = Duration,
            SpeedRatio = group.SpeedRatio ?? 1.0d,
            Easing = group.EasingFunction ?? _defaultEasing
        };
        foreach (var frame in group.Frames) animation.Children.Add(frame);
        return animation;
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
}