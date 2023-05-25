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
    private readonly AssetManager _assetManager;
    private readonly IFileBaseService _fileBase;
    private readonly LocalizationService _localizationService;
    private readonly INotificationService _notification;

    public InstanceAssetViewModel(
        ViewModelContext context,
        AssetManager assetManager,
        INotificationService notification,
        IFileBaseService fileBase,
        LocalizationService localizationService
    )
    {
        Instance = context.AssociatedInstance!;
        _notification = notification;
        _assetManager = assetManager;
        _fileBase = fileBase;
        _localizationService = localizationService;
        OpenInExplorerCommand = new RelayCommand<InstanceAssetModel>(OpenInExplorer);
        DeleteAssetCommand = new RelayCommand<InstanceAssetModel>(DeleteAsset);
        Assets = new ObservableCollection<InstanceAssetModel>();
    }

    public GameInstanceModel Instance { get; }
    public ObservableCollection<InstanceAssetModel> Assets { get; }
    public ResourceType? Type { get; set; }

    public string? TypeFriendlyName { get; set; }

    public ICommand OpenInExplorerCommand { get; }
    public ICommand DeleteAssetCommand { get; }

    public void SetType(ResourceType type)
    {
        Type = type;
        TypeFriendlyName = type switch
        {
            ResourceType.Mod => _localizationService.GetString("ResourceType_Mod"),
            ResourceType.ResourcePack
                => _localizationService.GetString("ResourceType_ResourcePack"),
            ResourceType.ShaderPack => _localizationService.GetString("ResourceType_ShaderPack"),
            _ => string.Empty
        };
    }

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
        var result = await _assetManager.ExtractAssetInfoAsync(raw, token);
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
        var product = await _assetManager.InstallAssetAsync(Instance.Inner, Type!.Value, url);
        if (product.HasValue)
        {
            callback(new AssetRaw { FileName = url, Type = Type!.Value });
            _notification.Enqueue(
                _localizationService.GetString("InstanceAssetViewModel_AddFile_Accepted_Caption"),
                _localizationService
                    .GetString("InstanceAssetViewModel_AddFile_Accepted_Message")
                    .Replace("{0}", product.Value.Name)
                    .Replace("{1}", Type!.Value.ToString()),
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
            _notification.Enqueue(
                _localizationService.GetString("InstanceAssetViewModel_AddFile_Rejected_Caption"),
                _localizationService.GetString("InstanceAssetViewModel_AddFile_Rejected_Caption"),
                InfoBarSeverity.Warning
            );
        }
    }

    public void OpenInExplorer(InstanceAssetModel? model)
    {
        var path =
            model != null
                ? _fileBase.Locate(model.Url)
                : _fileBase.Locate(_assetManager.GetAssetDirectory(Instance.Inner, Type!.Value));
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
                    _notification.Enqueue(
                        _localizationService.GetString(
                            "InstanceAssetViewModel_DeleteFile_Failure_Caption"
                        ),
                        e.Message,
                        InfoBarSeverity.Error
                    );
                }

            Assets.Remove(model);
        }
    }
}
