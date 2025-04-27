using Avalonia;
using Avalonia.Controls.Primitives;

namespace Polymerium.App.Controls;

public class ExhibitDependencyEntry : TemplatedControl
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ExhibitDependencyEntry, bool>(nameof(IsChecked));

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<ExhibitDependencyEntry, bool>(nameof(IsRequired));

    public bool IsRequired
    {
        get => GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }
}