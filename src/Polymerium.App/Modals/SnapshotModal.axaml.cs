using System;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Mixins;
using Microsoft.Extensions.DependencyInjection;

namespace Polymerium.App.Modals;

public partial class SnapshotModal : Modal
{
    public SnapshotModal()
    {
        InitializeComponent();

        FrameActivationMixin.Install(Frame, Program.Services!.GetRequiredService<IViewActivator>());
    }



    private void Navigate(Type page)
    {
        Frame.Navigate(page, null, null);
    }
}

