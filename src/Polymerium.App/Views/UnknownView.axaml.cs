using Avalonia.Interactivity;
using Avalonia.Media;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Views;

public partial class UnknownView : Page
{
    public UnknownView()
    {
        InitializeComponent();
    }

    private IBrush? _switch = Brushes.Transparent;

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        (Background, _switch) = (_switch, Background);
    }
}