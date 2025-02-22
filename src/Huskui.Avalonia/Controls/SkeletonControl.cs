using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading", ":animated")]
public class SkeletonControl : ContentControl
{
    public static readonly DirectProperty<SkeletonControl, bool> IsLoadingProperty = AvaloniaProperty.RegisterDirect<SkeletonControl, bool>(nameof(IsLoading), o => o.IsLoading, (o, v) => o.IsLoading = v);

    public static readonly DirectProperty<SkeletonControl, bool> IsAnimatedProperty = AvaloniaProperty.RegisterDirect<SkeletonControl, bool>(nameof(IsAnimated), o => o.IsAnimated, (o, v) => o.IsAnimated = v);

    private bool _isAnimated;

    private bool _isLoading;

    public SkeletonControl() =>
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