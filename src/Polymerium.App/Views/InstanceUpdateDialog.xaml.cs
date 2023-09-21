// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views;

public sealed partial class InstanceUpdateDialog : CustomDialog
{
    public InstanceUpdateDialog(
        GameInstanceModel instance,
        InstanceModpackReferenceModel modpack,
        InstanceModpackReferenceVersionModel version
    )
    {
        Instance = instance;
        Modpack = modpack;
        Version = version;
        ViewModel = App.Current.Provider.GetRequiredService<InstanceUpdateViewModel>();
        InitializeComponent();
    }

    public bool IsProcessing
    {
        get { return (bool)GetValue(IsProcessingProperty); }
        set { SetValue(IsProcessingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsProcessing.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsProcessingProperty = DependencyProperty.Register(
        nameof(IsProcessing),
        typeof(bool),
        typeof(InstanceUpdateDialog),
        new PropertyMetadata(false)
    );

    public bool ApplyLocalFileReset
    {
        get { return (bool)GetValue(ApplyLocalFileResetProperty); }
        set { SetValue(ApplyLocalFileResetProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ApplyLocalFileReset.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ApplyLocalFileResetProperty =
        DependencyProperty.Register(
            nameof(ApplyLocalFileReset),
            typeof(bool),
            typeof(InstanceUpdateDialog),
            new PropertyMetadata(true)
        );

    public InstanceUpdateViewModel ViewModel { get; }
    public GameInstanceModel Instance { get; }
    public InstanceModpackReferenceModel Modpack { get; }
    public InstanceModpackReferenceVersionModel Version { get; }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Dismiss();
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        IsProcessing = true;
        var reset = ApplyLocalFileReset;
        await Task.Run(
            () =>
                ViewModel.ApplyUpdateAsync(
                    Instance.Inner,
                    Version.Resource,
                    reset,
                    UpdateFinishHandler
                )
        );
    }

    private void UpdateFinishHandler(bool succ)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Dismiss();
            if (succ)
                ViewModel.NavigationService.Navigate<InstanceView>(Instance.Id);
        });
    }
}
