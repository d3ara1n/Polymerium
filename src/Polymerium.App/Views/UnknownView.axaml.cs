using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Views;

public partial class UnknownView : Page
{
    public UnknownView() => InitializeComponent();

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current != null)
        {
            if (Application.Current.ActualThemeVariant == ThemeVariant.Light)
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
            else
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
        }
    }
}