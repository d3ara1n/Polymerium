using System;
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

        Frame.Navigate(typeof(SnapshotPortalPage), null, null);
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
