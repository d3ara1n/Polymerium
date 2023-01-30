using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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

public class CreateInstanceWizardViewModel : ObservableValidator
{
    private readonly IMemoryCache _cache;
    private readonly InstanceManager _instanceManager;

    private string instanceAuthor = string.Empty;

    private string instanceName = string.Empty;

    private GameVersionModel selectedVersion;

    public CreateInstanceWizardViewModel(InstanceManager instanceManager, IMemoryCache cache)
    {
        _instanceManager = instanceManager;
        _cache = cache;
    }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string InstanceName
    {
        get => instanceName;
        set => SetProperty(ref instanceName, value, true);
    }

    public string InstanceAuthor
    {
        get => instanceAuthor;
        set => SetProperty(ref instanceAuthor, value);
    }

    [Required]
    public GameVersionModel SelectedVersion
    {
        get => selectedVersion;
        set => SetProperty(ref selectedVersion, value, true);
    }

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
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        var instance = new GameInstance
        {
            Id = Guid.NewGuid().ToString(),
            Name = InstanceName,
            FolderName = string.Join("", InstanceName.Select(x => invalidFileNameChars.Contains(x) ? '_' : x)),
            Author = InstanceAuthor,
            Metadata = new GameMetadata
            {
                Components = new[] { new Component { Identity = "net.minecraft", Version = SelectedVersion.Id } }
            }
        };
        _instanceManager.AddInstance(instance);
        await callback();
    }
}