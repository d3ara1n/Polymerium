using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels.Instances;
using Polymerium.Core.GameAssets;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceAssetDrawer : Drawer
{
    public bool IsParsing
    {
        get => (bool)GetValue(IsParsingProperty);
        set => SetValue(IsParsingProperty, value);
    }

    // Using a DependencyProperty as the backing store for IsParsing.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsParsingProperty =
        DependencyProperty.Register(nameof(IsParsing), typeof(bool), typeof(InstanceAssetDrawer),
            new PropertyMetadata(false));


    public InstanceAssetDrawer(ResourceType type, IAdvancedCollectionView view)
    {
        _type = type;
        _view = view;
        _source = new CancellationTokenSource();
        ViewModel = App.Current.Provider.GetRequiredService<InstanceAssetViewModel>();
        InitializeComponent();
        Title = type switch
        {
            ResourceType.Mod => "模组",
            ResourceType.ShaderPack => "着色器包",
            ResourceType.ResourcePack => "资源包",
            _ => throw new NotImplementedException()
        };
    }

    private readonly ResourceType _type;
    private readonly IAdvancedCollectionView _view;
    private readonly CancellationTokenSource _source;

    public InstanceAssetViewModel ViewModel { get; }

    public ObservableCollection<InstanceAssetModel> Assets { get; } = new();

    private void DragDropPane_DragEnter(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 1.0;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
            e.AcceptedOperation = DataPackageOperation.Link;
    }

    private void DragDropPane_DragLeave(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 0.3;
    }

    private async void DragDropPane_Drop(object sender, DragEventArgs e)
    {
        DragDropPane.Opacity = 0.3;
        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var file = items!.First()!;
            e.Handled = true;
            // TODO
        }
    }

    protected override void OnClosing()
    {
        _source.Cancel();
    }

    private void Drawer_Loaded(object sender, RoutedEventArgs e)
    {
        IsParsing = true;
        Task.Run(() => ViewModel.LoadAssetsAsync(_view.Select(x => (AssetRaw)x), AddAssetHandler, _source.Token));
    }

    private void AddAssetHandler(InstanceAssetModel? model)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
                Assets.Add(model);
            else
                IsParsing = false;
        });
    }
}