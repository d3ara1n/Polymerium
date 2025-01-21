using Avalonia.Interactivity;
using Avalonia.Media;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Views;

public partial class UnknownView : Page
{
    private IBrush? _switch = Brushes.Transparent;

    public UnknownView()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        (Background, _switch) = (_switch, Background);
    }
}