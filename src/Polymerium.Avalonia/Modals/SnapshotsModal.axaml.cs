using System;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Mixins;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.ModalModels;

namespace Polymerium.Avalonia.Modals;

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
                viewModel._navigateHandler = p => Frame.Navigate(p, ((SnapshotsModalModel)DataContext!).Context!);
                viewModel._backHandler = Frame.GoBack;
                viewModel._dismissHandler = Dismiss;
            }
        }
    }

    [RelayCommand]
    private void Navigate(Type? page)
    {
        var context = ((SnapshotsModalModel)DataContext!).Context!;
        if (page != null)
        {
            Frame.Navigate(page, context);
        }
    }
}
