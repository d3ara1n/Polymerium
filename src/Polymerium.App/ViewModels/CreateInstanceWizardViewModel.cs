using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Wupoo;

namespace Polymerium.App.ViewModels;

public class CreateInstanceWizardViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;
    private readonly IMemoryCache _cache;

    public CreateInstanceWizardViewModel(InstanceManager instanceManager, IMemoryCache cache)
    {
        _instanceManager = instanceManager;
        _cache = cache;
    }

    private string instanceName = string.Empty;
    public string InstanceName { get => instanceName; set => SetProperty(ref instanceName, value); }
    private string instanceAuthor = string.Empty;
    public string InstanceAuthor { get => instanceAuthor; set => SetProperty(ref instanceAuthor, value); }

    private GameVersionModel? selectedVersion;
    public GameVersionModel? SelectedVersion { get => selectedVersion; set => SetProperty(ref selectedVersion, value); }

    public async Task FillDataAsync(Func<IEnumerable<GameVersionModel>, Task> callback)
    {
        // TODO: 日后改成 ResourceResolver
        var versions = await _cache.GetOrCreateAsync("GetGameVersions", async entry =>
        {
            var res = Enumerable.Empty<GameVersionModel>();
            var url = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
            await Wapoo.Wohoo(url)
            .ForJsonResult<JObject>(x =>
            {
                res = x.Value<JArray>("versions").ToObject<IEnumerable<GameVersionModel>>();
            })
            .FetchAsync();
            return res;
        });
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
            ThumbnailFile = "ms-appx:///Assets/Placeholders/default_world_icon.png",
            Metadata = new()
            {
                CoreVersion = SelectedVersion.Id,
                Components = Enumerable.Empty<Component>()
            }
        };
        _instanceManager.AddInstance(instance);
        await callback();
    }
}
