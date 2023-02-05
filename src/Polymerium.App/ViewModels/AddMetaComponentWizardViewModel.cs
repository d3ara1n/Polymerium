using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Services;
using Polymerium.Core.Components;
using Polymerium.Core.Models.BmclApi.Forge;
using Polymerium.Core.Models.Mojang.VersionManifests;
using Wupoo;

namespace Polymerium.App.ViewModels;

public class AddMetaComponentWizardViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly IMemoryCache _cache;
    private readonly string coreVersion;

    public AddMetaComponentWizardViewModel(ViewModelContext context, ComponentManager componentManager,
        IMemoryCache cache)
    {
        Context = context;
        _componentManager = componentManager;
        _cache = cache;
        coreVersion = Context.AssociatedInstance.Components.Any(x => x.Identity == "net.minecraft")
            ? Context.AssociatedInstance.Components.First(x => x.Identity == "net.minecraft").Version
            : null;
        if (coreVersion != null)
            Metas = _componentManager.GetView(ComponentViewFilter.Modloader);
        else
            Metas = _componentManager.GetView(ComponentViewFilter.Core);

        AddComponentCommand = new RelayCommand(AddComponent, CanAddComponent);
        CancelCommand = new RelayCommand(Cancel);
    }

    internal Action DismissHandler;
    public IRelayCommand AddComponentCommand { get; }
    public ICommand CancelCommand { get; }
    public ViewModelContext Context { get; }
    public IEnumerable<ComponentMeta> Metas { get; }
    private ComponentMeta selectedMeta;

    private IEnumerable<string> versions;

    public IEnumerable<string> Versions
    {
        get => versions;
        set => SetProperty(ref versions, value);
    }

    public ComponentMeta SelectedMeta
    {
        get => selectedMeta;
        set => SetProperty(ref selectedMeta, value);
    }

    private string selectedVersion;

    public string SelectedVersion
    {
        get => selectedVersion;
        set
        {
            SetProperty(ref selectedVersion, value);
            AddComponentCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanAddComponent()
    {
        return SelectedVersion != null;
    }

    private void AddComponent()
    {
        var identity = SelectedMeta.Identity;
        var version = SelectedVersion;
        Context.AssociatedInstance.Components.Add(new Component
        {
            Identity = identity,
            Version = version
        });
        DismissHandler?.Invoke();
    }

    private void Cancel()
    {
        DismissHandler?.Invoke();
    }

    public async Task LoadVersionsAsync(string identity, Action<IEnumerable<string>> callback)
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<string>>($"versions:{identity}", identity switch
        {
            "net.minecraft" => LoadMinecraftVersionsAsync,
            "net.minecraftforge" => LoadForgeVersionsAsync,
            _ => _ => Task.FromResult(Enumerable.Empty<string>())
        });
        callback(result);
    }

    private async Task<IEnumerable<string>> LoadVersionsAsync<TMid>(string url, Func<TMid, IEnumerable<string>> produce,
        ICacheEntry entry)
    {
        var result = Enumerable.Empty<string>();
        await Wapoo.Wohoo(url)
            .ForJsonResult<TMid>(x => result = produce(x))
            .FetchAsync();
        if (result.Any())
            entry.SetSlidingExpiration(TimeSpan.FromHours(1));
        else
            entry.SetSlidingExpiration(TimeSpan.FromSeconds(1));

        return result;
    }

    private async Task<IEnumerable<string>> LoadMinecraftVersionsAsync(ICacheEntry entry)
    {
        return await LoadVersionsAsync<JObject>("https://piston-meta.mojang.com/mc/game/version_manifest.json",
            x => x.Value<JArray>("versions").ToObject<IEnumerable<GameVersion>>().Select(x => x.Id),
            entry);
    }


    private async Task<IEnumerable<string>> LoadForgeVersionsAsync(ICacheEntry entry)
    {
        return await LoadVersionsAsync<IEnumerable<ForgeBuild>>(
            $"https://bmclapi2.bangbang93.com/forge/minecraft/{coreVersion}",
            x => x.Select(x => x.Version).OrderDescending(), entry);
    }
}