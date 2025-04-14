using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Huskui.Avalonia.Controls;

namespace Huskui.Gallery;

public partial class MainWindow : AppWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ChangeTheme_OnClick(object? sender, RoutedEventArgs e)
    {
        Application.Current!.RequestedThemeVariant = Application.Current.ActualThemeVariant == ThemeVariant.Light
                                                         ? ThemeVariant.Dark
                                                         : ThemeVariant.Light;
    }
}