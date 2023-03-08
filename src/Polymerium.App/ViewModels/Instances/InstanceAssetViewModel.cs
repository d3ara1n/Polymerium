using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;
using Polymerium.Core;
using Polymerium.Core.GameAssets;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceAssetViewModel : ObservableObject
{
    private readonly GameManager _gameManager;

    public InstanceAssetViewModel(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public async Task LoadAssetsAsync(IEnumerable<AssetRaw> assets, Action<InstanceAssetModel?> callback,
        CancellationToken token = default)
    {
        var tasks = new List<Task>();
        foreach (var raw in assets) tasks.Add(LoadAssetAsync(raw, callback, token));
        await Task.WhenAll(tasks);
        callback(null);
    }

    private async Task LoadAssetAsync(AssetRaw raw, Action<InstanceAssetModel?> callback,
        CancellationToken token = default)
    {
        if (token.IsCancellationRequested) return;
        var result = await _gameManager.ExtractAssetInfoAsync(raw, token);
        if (result.HasValue)
        {
            var model = new InstanceAssetModel(result.Value.Type, result.Value.FileName, result.Value.Name,
                result.Value.Version, result.Value.Description);
            callback(model);
        }
    }
}