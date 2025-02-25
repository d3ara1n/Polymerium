using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading", ":animated")]
public class SkeletonContainer : ContentControl
{
    public static readonly DirectProperty<SkeletonContainer, bool> IsLoadingProperty =
        AvaloniaProperty.RegisterDirect<SkeletonContainer, bool>(nameof(IsLoading),
                                                                 o => o.IsLoading,
                                                                 (o, v) => o.IsLoading = v);

    public static readonly DirectProperty<SkeletonContainer, bool> IsAnimatedProperty =
        AvaloniaProperty.RegisterDirect<SkeletonContainer, bool>(nameof(IsAnimated),
                                                                 o => o.IsAnimated,
                                                                 (o, v) => o.IsAnimated = v);

    private bool _isAnimated;

    private bool _isLoading;

    public SkeletonContainer() =>
        // Default Property Value
        IsAnimated = true;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetAndRaise(IsLoadingProperty, ref _isLoading, value))
                PseudoClasses.Set(":loading", value);
        }
    }

    public bool IsAnimated
    {
        get => _isAnimated;
        set
        {
            if (SetAndRaise(IsAnimatedProperty, ref _isAnimated, value))
                PseudoClasses.Set(":animated", value);
        }
    }
}