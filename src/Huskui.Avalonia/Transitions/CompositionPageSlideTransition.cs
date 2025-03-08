using System.Numerics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Rendering.Composition;

namespace Huskui.Avalonia.Transitions;

public class CompositionPageSlideTransition : IPageTransition
{
    public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (from != null)
        {
            var visual = ElementComposition.GetElementVisual(from);
            if (visual != null)
            {
                var compositor = visual.Compositor;
                var opacity = compositor.CreateScalarKeyFrameAnimation();
                opacity.InsertKeyFrame(0, 1.0f);
                opacity.InsertKeyFrame(1, 0.0f);

                opacity.Duration = TimeSpan.FromMilliseconds(297);
                visual.StartAnimation("Opacity", opacity);
            }
        }

        if (to != null)
        {
            var visual = ElementComposition.GetElementVisual(to);
            if (visual != null)
            {
                var compositor = visual.Compositor;
                var opacity = compositor.CreateScalarKeyFrameAnimation();
                opacity.InsertKeyFrame(0.0f, 0.0f);
                opacity.InsertKeyFrame(1.0f, 1.0f);

                opacity.Duration = TimeSpan.FromMilliseconds(297);
                visual.StartAnimation("Opacity", opacity);

                var slide = compositor.CreateVector3KeyFrameAnimation();
                slide.InsertKeyFrame(0, new Vector3(0f, (float)visual.Size.Y, 0f));
                slide.InsertKeyFrame(1, new Vector3(0f, 0f, 0f));
                slide.Duration = TimeSpan.FromMilliseconds(297);
                visual.StartAnimation("Offset", slide);
            }
        }

        return Task.Delay(TimeSpan.FromMilliseconds(297), cancellationToken);
    }
}