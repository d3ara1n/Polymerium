using Avalonia.Interactivity;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.PageModels;

namespace Polymerium.Avalonia.Pages;

public partial class InstanceHomePage : Subpage
{
    public InstanceHomePage() => InitializeComponent();

    private void Timer_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InstanceHomePageModel model)
        {
            model.ViewForTimerLaunch();
        }
    }

    private void Timer_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InstanceHomePageModel model)
        {
            model.ViewForTimerDestruct();
        }
    }
}
