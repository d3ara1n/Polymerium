using Avalonia;
using Avalonia.Animation;
using Avalonia.VisualTree;

namespace Huskui.Avalonia.Transitions;

public static class PageTransitionExtensions
{
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
    internal static Visual GetVisualParent(this IPageTransition self, Visual? from, Visual? to)
    {
        var p1 = (from ?? to)!.GetVisualParent();
        var p2 = (to ?? from)!.GetVisualParent();

        if (p1 != null && p2 != null && p1 != p2)
            throw new ArgumentException(
                "Controls for PageSlide must have same parent.");

        return p1 ?? throw new InvalidOperationException(
            "Cannot determine visual parent.");
    }
}