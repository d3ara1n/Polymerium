using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Polymerium.Abstractions.Resources;
using Polymerium.App.ViewModels;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.Views;

public sealed partial class InstanceView : Page
{
    public bool IsPending
    {
        get => (bool)GetValue(IsPendingProperty);
        set => SetValue(IsPendingProperty, value);
    }

    // Using a DependencyProperty as the backing store for IsPending.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsPendingProperty =
        DependencyProperty.Register(nameof(IsPending), typeof(bool), typeof(InstanceView), new PropertyMetadata(false));



    public InstanceView()
    {
        ViewModel = App.Current.Provider.GetRequiredService<InstanceViewModel>();
        InitializeComponent();
    }

    public InstanceViewModel ViewModel { get; }

    private void Header_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 1.0;
    }

    private void Header_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        EditButton.Opacity = 0.0;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadAssets();
        Dialog_Dismissed(null, new EventArgs());
    }

    private void LoadInformationHandler(Uri? url, bool isNeeded)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ViewModel.IsRestorationNeeded = isNeeded;
            ViewModel.ReferenceUrl = url;
            IsPending = false;
        });
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrepareGameDialog(ViewModel.Context.AssociatedInstance!.Inner, ViewModel.OverlayService);
        dialog.Dismissed += Dialog_Dismissed;
        ViewModel.OverlayService.Show(dialog);
    }

    private void Dialog_Dismissed(object? sender, EventArgs e)
    {
        ViewModel.LoadAssets();
        IsPending = true;
        Task.Run(() => ViewModel.LoadInstanceInformationAsync(LoadInformationHandler));
    }

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