using Avalonia;
using Avalonia.Controls;

namespace Polymerium.App.Controls
{
    public class InstancePackageDependencyButton : Button
    {
        public static readonly StyledProperty<bool> IsCheckedProperty =
            AvaloniaProperty.Register<ExhibitDependencyButton, bool>(nameof(IsChecked));

        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }
    }
}
