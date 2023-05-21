using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Humanizer;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views.Instances;
using Polymerium.Core;
using Polymerium.Core.Components;
using Polymerium.Core.Engines;
using Polymerium.Core.Extensions;
using Polymerium.Core.GameAssets;
using Polymerium.Core.Managers;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly AssetManager _assetManager;
    private readonly ComponentManager _componentManager;
    private readonly IFileBaseService _fileBase;
    private readonly LocalizationService _localizationService;
    private readonly NavigationService _navigationService;
    private readonly ResolveEngine _resolver;

    private string coreVersion = string.Empty;

    private bool isModSupported;

    private bool isRestorationNeeded;
    private bool isShaderSupported;
    private uint modCount;
    private Uri? referenceUrl;
    private uint resourcePackCount;
    private uint shaderPackCount;

    public InstanceViewModel(
        ViewModelContext context,
        InstanceManager instanceManager,
        ResolveEngine resolver,
        IOverlayService overlayService,
        IFileBaseService fileBase,
        ComponentManager componentManager,
        NavigationService navigationService,
        AssetManager assetManager,
        LocalizationService localizationService
    )
    {
        _componentManager = componentManager;
        _resolver = resolver;
        _navigationService = navigationService;
        _assetManager = assetManager;
        _fileBase = fileBase;
        _localizationService = localizationService;
        Instance = context.AssociatedInstance!;
        OverlayService = overlayService;
        CoreVersion = Instance.Inner.GetCoreVersion() ?? "N/A";
        GotoConfigurationViewCommand = new RelayCommand(GotoConfigurationView);
        Components = new ObservableCollection<ComponentTagItemModel>(
            BuildComponentModels(Instance.Components)
        );
        InformationItems = new ObservableCollection<InstanceInformationItemModel>
        {
            new("\uF427", _localizationService.GetString("InstanceView_Other_Identity_Label"), Instance.Id),
            new("\uE125", _localizationService.GetString("InstanceView_Other_Author_Label"),
                string.IsNullOrEmpty(Instance.Author)
                    ? _localizationService.GetString("InstanceView_Other_Author_Unknown")
                    : Instance.Author),
            new("\uE121", _localizationService.GetString("InstanceView_Other_PlayTime_Label"),
                Instance.PlayTime.Humanize()),
            new(
                "\uEC92",
                _localizationService.GetString("InstanceView_Other_LastPlay_Label"),
                Instance.LastPlay == null
                    ? _localizationService.GetString("InstanceView_Other_LastPlay_Unknown")
                    : Instance.LastPlay.Humanize()
            ),
            new("\uEB50", _localizationService.GetString("InstanceView_Other_PlayCount_Label"),
                $"{Instance.PlayCount}"),
            new(
                "\uEB05",
                _localizationService.GetString("InstanceView_Other_SuccessRate_Label"),
                Instance.PlayCount == 0
                    ? _localizationService.GetString("InstanceView_Other_SuccessRate_Unknown")
                    : $"{(Instance.PlayCount - Instance.ExceptionCount) / (float)Instance.PlayCount * 100}%"
            ),
            new("\uEC92", _localizationService.GetString("InstanceView_Other_CreateDate_Label"),
                Instance.CreatedAt.Humanize()),
            new(
                "\uEC92",
                _localizationService.GetString("InstanceView_Other_LastRestore_Label"),
                Instance.LastRestore == null
                    ? _localizationService.GetString("InstanceView_Other_LastRestore_Unknown")
                    : Instance.LastRestore.Humanize()
            )
        };
        OpenInExplorerCommand = new RelayCommand<string>(OpenInExplorer);
        Saves = new ObservableCollection<InstanceWorldSaveModel>();
        Screenshots = new ObservableCollection<InstanceScreenshotModel>();
        RawAssetSource = new ObservableCollection<AssetRaw>();
        RawShaderPacks = new AdvancedCollectionView
        {
            Source = RawAssetSource,
            Filter = x => ((AssetRaw)x).Type == ResourceType.ShaderPack
        };
        RawMods = new AdvancedCollectionView
        {
            Source = RawAssetSource,
            Filter = x => ((AssetRaw)x).Type == ResourceType.Mod
        };
        RawResourcePacks = new AdvancedCollectionView
        {
            Source = RawAssetSource,
            Filter = x => ((AssetRaw)x).Type == ResourceType.ResourcePack
        };
        IsModSupported = Instance.Components.Any(x => ComponentMeta.MINECRAFT != x.Identity);
        IsShaderSupported = true;
    }

    public string CoreVersion
    {
        get => coreVersion;
        set => SetProperty(ref coreVersion, value);
    }

    public ObservableCollection<ComponentTagItemModel> Components { get; }
    public ObservableCollection<InstanceInformationItemModel> InformationItems { get; }
    public ObservableCollection<InstanceWorldSaveModel> Saves { get; }
    public ObservableCollection<InstanceScreenshotModel> Screenshots { get; }
    public ObservableCollection<AssetRaw> RawAssetSource { get; }
    public IAdvancedCollectionView RawShaderPacks { get; }
    public IAdvancedCollectionView RawMods { get; }
    public IAdvancedCollectionView RawResourcePacks { get; }
    public IRelayCommand<string> OpenInExplorerCommand { get; }

    public bool IsModSupported
    {
        get => isModSupported;
        set => SetProperty(ref isModSupported, value);
    }

    public bool IsShaderSupported
    {
        get => isShaderSupported;
        set => SetProperty(ref isShaderSupported, value);
    }

    public uint ModCount
    {
        get => modCount;
        set => SetProperty(ref modCount, value);
    }

    public uint ResourcePackCount
    {
        get => resourcePackCount;
        set => SetProperty(ref resourcePackCount, value);
    }

    public uint ShaderPackCount
    {
        get => shaderPackCount;
        set => SetProperty(ref shaderPackCount, value);
    }

    public bool IsRestorationNeeded
    {
        get => isRestorationNeeded;
        set => SetProperty(ref isRestorationNeeded, value);
    }

    public Uri? ReferenceUrl
    {
        get => referenceUrl;
        set => SetProperty(ref referenceUrl, value);
    }

    public GameInstanceModel Instance { get; }
    public IOverlayService OverlayService { get; }
    public ICommand GotoConfigurationViewCommand { get; }

    private void OpenInExplorer(string? dir)
    {
        var path = Path.Combine(_fileBase.Locate(new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", Instance.Id))), dir);
        Process.Start(
            new ProcessStartInfo("explorer.exe")
            {
                UseShellExecute = true,
                Arguments = Directory.Exists(path) ? path : $"/select, {path}"
            }
        );
    }

    private IEnumerable<ComponentTagItemModel> BuildComponentModels(
        IEnumerable<Component> components
    )
    {
        return components.Select(x =>
        {
            _componentManager.TryFindByIdentity(x.Identity, out var meta);
            return new ComponentTagItemModel(
                meta?.FriendlyName ?? x.Identity,
                x.Version,
                x.Identity,
                $"{x.Identity}:{x.Version}"
            );
        });
    }

    public void GotoConfigurationView()
    {
        _navigationService.Navigate<InstanceConfigurationView>(new SlideNavigationTransitionInfo
            { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    public void LoadAssets()
    {
        var assets = _assetManager.ScanAssets(Instance.Inner);
        RawAssetSource.Clear();
        foreach (var asset in assets)
            RawAssetSource.Add(asset);
        RawMods.Refresh();
        ModCount = (uint)RawMods.Count;
        RawResourcePacks.Refresh();
        ResourcePackCount = (uint)RawResourcePacks.Count;
        RawShaderPacks.Refresh();
        ShaderPackCount = (uint)RawShaderPacks.Count;
    }

    public void LoadSaves()
    {
        var saves = _assetManager.ScanSaves(Instance.Inner);
        Saves.Clear();
        foreach (var save in saves)
        {
            var model = new InstanceWorldSaveModel(
                _fileBase.Locate(
                    new Uri(
                        new Uri(ConstPath.INSTANCE_BASE.Replace("{0}", Instance.Id)),
                        $"saves/{save.FolderName}/icon.png"
                    )
                ),
                save.Name,
                save.Seed,
                save.GameVersion,
                save.LastPlayed,
                save
            );
            Saves.Add(model);
        }
    }

    public void LoadScreenshots()
    {
        var screenshots = _assetManager.ScanScreenshots(Instance.Inner);
        Screenshots.Clear();
        foreach (var screenshot in screenshots)
        {
            var model = new InstanceScreenshotModel(screenshot.FileName);
            Screenshots.Add(model);
        }
    }

    public async Task LoadInstanceInformationAsync(Action<Uri?, bool> callback)
    {
        var isNeeded = !Instance.Inner.CheckIfRestored(_fileBase, out _);
        Uri? url = null;
        if (Instance.ReferenceSource != null)
        {
            var result = await _resolver.ResolveAsync(
                Instance.ReferenceSource,
                new ResolverContext(Instance.Inner)
            );
            if (result)
                url = result.Value.Resource.Reference;
        }

        callback(url, isNeeded);
    }
}