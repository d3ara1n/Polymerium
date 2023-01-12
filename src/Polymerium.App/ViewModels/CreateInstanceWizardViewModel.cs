using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Shapes;
using Polymerium.Abstractions;
using Polymerium.Abstractions.DownloadSources;
using Polymerium.Abstractions.DownloadSources.Models;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Services;

namespace Polymerium.App.ViewModels;

public class CreateInstanceWizardViewModel : ObservableObject
{
    private readonly IEnumerable<DownloadSourceProviderBase> _providers;
    private readonly AssetStorageService _assetStorage;

    public CreateInstanceWizardViewModel(IEnumerable<DownloadSourceProviderBase> providers, AssetStorageService storageService)
    {
        _providers = providers;
        _assetStorage = storageService;
    }

    private string instanceName = string.Empty;
    public string InstanceName { get => instanceName; set => SetProperty(ref instanceName, value); }
    private string instanceAuthor = string.Empty;
    public string InstanceAuthor { get => instanceAuthor; set => SetProperty(ref instanceAuthor, value); }

    private GameVersion? selectedVersion;
    public GameVersion? SelectedVersion { get => selectedVersion; set => SetProperty(ref selectedVersion, value); }

    public async Task FillDataAsync(Func<IEnumerable<GameVersion>, Task> callback)
    {
        // TODO: 走缓存
        var versions = Enumerable.Empty<GameVersion>();
        foreach (var provider in _providers)
        {
            var versions_option = await provider.GetGameVersionsAsync();
            if (versions_option.TryUnwrap(out var data))
            {
                versions = data;
                break;
            }
        }
        await callback(versions.ToList());
    }

    public async Task Commit(Func<Task> callback)
    {
        // TODO: 检查一下元素是否合法，返回错误，如果添加失败也返回错误
        // TODO: 添加所谓的 ValidationRule
        var invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
        var instance = new GameInstance()
        {
            Id = Guid.NewGuid().ToString(),
            Name = InstanceName,
            FolderName = string.Join("", InstanceName.Select(x => invalidFileNameChars.Contains(x) ? '_' : x)),
            Author = InstanceAuthor,
            Metadata = new()
            {
                CoreVersion = SelectedVersion.Value.Id,
                Extenders = Enumerable.Empty<ExperienceExtender>()
            }
        };
        _assetStorage.AddInstance(instance);
        await callback();
    }
}
