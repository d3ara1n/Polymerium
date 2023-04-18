using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Core;
using Polymerium.Core.GameAssets;
using Polymerium.Core.Managers;
using File = System.IO.File;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceAssetViewModel : ObservableObject
{
    private readonly IFileBaseService _fileBase;
    private readonly AssetManager _gameManager;
    private readonly INotificationService _notification;

    public InstanceAssetViewModel(
        ViewModelContext context,
        AssetManager gameManager,
        INotificationService notification,
        IFileBaseService fileBase
    )
    {
        Instance = context.AssociatedInstance!;
        _notification = notification;
        _gameManager = gameManager;
        _fileBase = fileBase;
        OpenInExplorerCommand = new RelayCommand<InstanceAssetModel>(OpenInExplorer);
        DeleteAssetCommand = new RelayCommand<InstanceAssetModel>(DeleteAsset);
        Assets = new ObservableCollection<InstanceAssetModel>();
    }

    public GameInstanceModel Instance { get; }
    public ObservableCollection<InstanceAssetModel> Assets { get; }

    public ResourceType? Type { get; set; }

    public ICommand OpenInExplorerCommand { get; }
    public ICommand DeleteAssetCommand { get; }

    public async Task LoadAssetsAsync(
        IEnumerable<AssetRaw> assets,
        Action<InstanceAssetModel?> callback,
        CancellationToken token = default
    )
    {
        var tasks = new List<Task>();
        foreach (var raw in assets)
            tasks.Add(LoadAssetAsync(raw, callback, token));
        await Task.WhenAll(tasks);
        callback(null);
    }

    private async Task LoadAssetAsync(
        AssetRaw raw,
        Action<InstanceAssetModel?> callback,
        CancellationToken token = default
    )
    {
        if (token.IsCancellationRequested)
            return;
        var result = await _gameManager.ExtractAssetInfoAsync(raw, token);
        if (result.HasValue)
        {
            var model = new InstanceAssetModel(
                result.Value.Type,
                result.Value.FileName,
                result.Value.Name,
                result.Value.Version,
                result.Value.Description
            );
            callback(model);
        }
    }

    public async Task FileAccepted(string fileName, Action<AssetRaw> callback)
    {
        var url = new Uri(fileName);
        var product = await _gameManager.InstallAssetAsync(Instance.Inner, Type!.Value, url);
        if (product.HasValue)
        {
            callback(new AssetRaw { FileName = url, Type = Type!.Value });
            _notification.Enqueue(
                "添加成功",
                $"{product.Value.Name} 作为 {Type!.Value} 被添加",
                InfoBarSeverity.Success
            );
            var model = new InstanceAssetModel(
                Type!.Value,
                url,
                product.Value.Name,
                product.Value.Version,
                product.Value.Description
            );
            Assets.Add(model);
        }
        else
        {
            _notification.Enqueue("添加失败", "选择的包无法识别，可打开目录手动添加", InfoBarSeverity.Warning);
        }
    }

    public void OpenInExplorer(InstanceAssetModel? model)
    {
        var path =
            model != null
                ? _fileBase.Locate(model.Url)
                : _fileBase.Locate(_gameManager.GetAssetDirectory(Instance.Inner, Type!.Value));
        Process.Start(
            new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = true,
                Arguments = Directory.Exists(path) ? path : $"/select, {path}"
            }
        );
    }

    public void DeleteAsset(InstanceAssetModel? model)
    {
        if (model != null)
        {
            var file = model.Url;
            var fileName = _fileBase.Locate(file);
            if (File.Exists(fileName))
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception e)
                {
                    _notification.Enqueue("删除失败", e.Message, InfoBarSeverity.Error);
                }

            Assets.Remove(model);
        }
    }
}
