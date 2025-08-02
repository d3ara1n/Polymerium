using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Interactivity;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;
using Polymerium.App.Widgets;

namespace Polymerium.App.Views;

public partial class InstanceHomeView : Subpage
{
    public InstanceHomeView()
    {
        InitializeComponent();
    }

    private void Timer_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InstanceHomeViewModel model)
            model.ViewForTimerLaunch();
    }

    private void Timer_OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is InstanceHomeViewModel model)
            model.ViewForTimerDestruct();
    }
}