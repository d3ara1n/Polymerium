using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Core.Models;
using Polymerium.Core.Models.Mojang.VersionManifests;
using Wupoo;

namespace Polymerium.App.ViewModels;

public class CreateInstanceWizardViewModel : ObservableValidator
{
    private readonly IMemoryCache _cache;
    private readonly InstanceManager _instanceManager;

    private string? autoSelectedVersion = string.Empty;

    private string instanceAuthor = string.Empty;

    private string instanceName = string.Empty;

    private GameVersionModel? selectedVersion;

    private string version = string.Empty;

    public CreateInstanceWizardViewModel(InstanceManager instanceManager, IMemoryCache cache)
    {
        _instanceManager = instanceManager;
        _cache = cache;

        InstanceName = string.Empty;
        SelectedVersion = null;
        InstanceAuthor = string.Empty;
    }

    [Required]
    [RegularExpression(@"^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)*$")]
    public string Version
    {
        get => version;
        set => SetProperty(ref version, value);
    }

    [Required]
    [MinLength(1)]
    [MaxLength(128)]
    public string InstanceName
    {
        get => instanceName;
        set => SetProperty(ref instanceName, value, true);
    }

    [MaxLength(128)]
    public string InstanceAuthor
    {
        get => instanceAuthor;
        set => SetProperty(ref instanceAuthor, value);
    }

    [Required]
    public GameVersionModel? SelectedVersion
    {
        get => selectedVersion;
        set
        {
            if (SetProperty(ref selectedVersion, value, true))
                if (value != null && InstanceName == autoSelectedVersion)
                {
                    InstanceName = value.Id;
                    autoSelectedVersion = value.Id;
                }
        }
    }

    public async Task FillDataAsync(Func<IEnumerable<GameVersionModel>, Task> callback)
    {
        var versions = await _cache.GetOrCreateAsync(
            "versions:core",
            async entry =>
            {
                var res = Enumerable.Empty<GameVersionModel>();
                var url = "https://piston-meta.mojang.com/mc/game/version_manifest.json";
                await Wapoo
                    .Wohoo(url)
                    .ForJsonResult<JObject>(
                        x =>
                            res = x.Value<JArray>("versions")!
                                .ToObject<IEnumerable<GameVersion>>()!
                                .Select(
                                    x =>
                                        new GameVersionModel(
                                            x.Id,
                                            x.Type switch
                                            {
                                                ReleaseType.Release => "正式",
                                                ReleaseType.Snapshot => "快照",
                                                ReleaseType.Old_Alpha => "Alpha",
                                                ReleaseType.Old_Beta => "Beta",
                                                _ => throw new ArgumentOutOfRangeException()
                                            }
                                        )
                                )
                    )
                    .FetchAsync();
                var gameVersionModels = res as GameVersionModel[] ?? res.ToArray();
                if (gameVersionModels.Any())
                    entry.SetSlidingExpiration(TimeSpan.FromHours(1));
                else
                    entry.SetSlidingExpiration(TimeSpan.FromSeconds(1));

                return gameVersionModels;
            }
        );
        await callback(versions?.ToList() ?? Enumerable.Empty<GameVersionModel>());
    }

    public async Task Commit(Func<Task> callback)
    {
        var instance = new GameInstance(
            new GameMetadata(),
            Version,
            new FileBasedLaunchConfiguration(),
            InstanceName,
            InstanceName
        );
        instance.Metadata.Components.Add(
            new Component { Identity = "net.minecraft", Version = SelectedVersion!.Id }
        );
        _instanceManager.AddInstance(instance);
        await callback();
    }
}