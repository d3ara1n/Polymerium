using Avalonia.Interactivity;
using Polymerium.App.Controls;
using Polymerium.App.PageModels;

namespace Polymerium.App.Pages;

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
