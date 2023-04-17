using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels.Instances;
using Polymerium.Core.GameAssets;
using File = System.IO.File;

namespace Polymerium.App.Views.Instances;

public sealed partial class InstanceAssetDrawer : Drawer
{
    // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(InstanceAssetDrawer),
        new PropertyMetadata(string.Empty)
    );

    // Using a DependencyProperty as the backing store for IsParsing.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsParsingProperty = DependencyProperty.Register(
        nameof(IsParsing),
        typeof(bool),
        typeof(InstanceAssetDrawer),
        new PropertyMetadata(false)
    );

    private readonly CancellationTokenSource _source;

    private readonly IAdvancedCollectionView _view;

    public InstanceAssetDrawer(ResourceType type, IAdvancedCollectionView view)
    {
        _view = view;
        _source = new CancellationTokenSource();
        ViewModel = App.Current.Provider.GetRequiredService<InstanceAssetViewModel>();
        ViewModel.Type = type;
        InitializeComponent();
        Title = type switch
        {
            ResourceType.Mod => "模组",
            ResourceType.ShaderPack => "着色器包",
            ResourceType.ResourcePack => "资源包",
            _ => throw new NotImplementedException()
        };
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsParsing
    {
        get => (bool)GetValue(IsParsingProperty);
        set => SetValue(IsParsingProperty, value);
    }

    public InstanceAssetViewModel ViewModel { get; }

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
            if (File.Exists(file.Path))
                await ViewModel.FileAccepted(file.Path, raw => _view.Add(raw));
        }
    }

    protected override void OnClosing()
    {
        _source.Cancel();
    }

    private void Drawer_Loaded(object sender, RoutedEventArgs e)
    {
        IsParsing = true;
        Task.Run(
            () =>
                ViewModel.LoadAssetsAsync(
                    _view.Select(x => (AssetRaw)x),
                    AddAssetHandler,
                    _source.Token
                )
        );
    }

    private void AddAssetHandler(InstanceAssetModel? model)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (model != null)
                ViewModel.Assets.Add(model);
            else
                IsParsing = false;
        });
    }

    private void AssetSearch_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args
    )
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            AssetsSource.Filter = obj =>
                ((InstanceAssetModel)obj).Name.StartsWith(
                    sender.Text,
                    StringComparison.OrdinalIgnoreCase
                );
            AssetsSource.RefreshFilter();
        }
    }
}
