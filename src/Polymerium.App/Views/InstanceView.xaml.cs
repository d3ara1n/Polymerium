using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.Resources;
using Polymerium.App.ViewModels;
using Polymerium.App.Views.Instances;
using Polymerium.Core.Managers.GameModels;
using System;
using System.Threading.Tasks;

namespace Polymerium.App.Views;

public sealed partial class InstanceView : Page
{
    // Using a DependencyProperty as the backing store for IsPending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsPendingProperty = DependencyProperty.Register(
        nameof(IsPending),
        typeof(bool),
        typeof(InstanceView),
        new PropertyMetadata(false)
    );

    public InstanceView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<InstanceViewModel>();
        InitializeComponent();
        ViewModel.SetStateChangeHandler(StateChangeHandler);
    }

    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    public InstanceViewModel ViewModel { get; }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadSaves();
        ViewModel.LoadScreenshots();
        ViewModel.LoadAssets();
        StateChangeHandler(ViewModel.QueryInstanceState(PrepareCallbackHandler));
        IsPending = true;
        Task.Run(() => ViewModel.LoadInstanceInformationAsync(LoadInformationHandler));
    }

    private void StateChangeHandler(InstanceState state) =>
        DispatcherQueue.TryEnqueue(() =>
        {
            VisualStateManager.GoToState(this, state.ToString(), false);
            RestoreMarquee.Switch(
                state switch
                {
                    InstanceState.Preparing => 2,
                    InstanceState.Running => 1,
                    InstanceState.Idle => -1,
                    _ => 1
                }
            );
        });

    private void LoadInformationHandler(Uri? url, bool isNeeded)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (ViewModel.QueryInstanceState(PrepareCallbackHandler) == InstanceState.Idle)
                RestoreMarquee.Switch(isNeeded ? 0 : 1);
            ViewModel.ReferenceUrl = url;
            IsPending = false;
        });
    }

    private void StartButton_Click(object _, RoutedEventArgs __)
    {
        Task.Run(() => ViewModel.TryStart(PrepareCallbackHandler));
    }

    private void PrepareCallbackHandler(int? precentage) =>
        DispatcherQueue.TryEnqueue(() =>
        {
            StartButton_Progress.IsIndeterminate = !precentage.HasValue;
            if (precentage.HasValue)
            {
                StartButton_Progress.Value = precentage.Value;
            }
        });

    private void OpenAssetDrawer(ResourceType type, IAdvancedCollectionView view)
    {
        var drawer = new InstanceAssetDrawer(type, view)
        {
            OverlayService = ViewModel.OverlayService
        };
        drawer.Closed += Drawer_Closed;
        ViewModel.OverlayService.Show(drawer);
    }

    private void Drawer_Closed(object? sender, EventArgs e)
    {
        ViewModel.LoadAssets();
    }

    private void OpenResourcePackButton_Click(object sender, RoutedEventArgs e)
    {
        OpenAssetDrawer(ResourceType.ResourcePack, ViewModel.RawResourcePacks);
    }

    private void OpenModButton_Click(object sender, RoutedEventArgs e)
    {
        OpenAssetDrawer(ResourceType.Mod, ViewModel.RawMods);
    }

    private void OpenShaderPackButton_Click(object sender, RoutedEventArgs e)
    {
        OpenAssetDrawer(ResourceType.ShaderPack, ViewModel.RawShaderPacks);
    }
}
