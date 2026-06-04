using System;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Mixins;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.ModalModels;
using Polymerium.App.Pages;

namespace Polymerium.App.Modals;

public partial class SnapshotsModal : Modal
{
    public SnapshotsModal()
    {
        InitializeComponent();

        FrameActivationMixin.Install(Frame, Program.Services!.GetRequiredService<IViewActivator>());
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataContextProperty)
        {
            if (change.NewValue is SnapshotsModalModel viewModel)
            {
                viewModel.NavigateHandler = (p) => Frame.Navigate(p, ((SnapshotsModalModel)DataContext!).Context!, null);
                viewModel.BackHandler = Frame.GoBack;
                viewModel.DismissHandler = Dismiss;
            }
        }
    }

    [RelayCommand]
    private void Navigate(Type? page)
    {
        var context = ((SnapshotsModalModel)DataContext!).Context!;
        if (page != null)
        {
            Frame.Navigate(page, context, null);
        }
    }
}
