using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":loading", ":animated")]
public class SkeletonContainer : ContentControl
{
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<SkeletonContainer, bool>(nameof(IsLoading));

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }


    public static readonly StyledProperty<bool> IsAnimatedProperty =
        AvaloniaProperty.Register<SkeletonContainer, bool>(nameof(IsAnimated));

    public bool IsAnimated
    {
        get => GetValue(IsAnimatedProperty);
        set => SetValue(IsAnimatedProperty, value);
    }

    public SkeletonContainer()
    {
        // Default Property Value
        IsAnimated = true;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsLoadingProperty)
        {
            PseudoClasses.Set(":loading", change.GetNewValue<bool>());
        }

        else if (change.Property == IsAnimatedProperty)
        {
            PseudoClasses.Set(":animated", change.GetNewValue<bool>());
        }
    }
}