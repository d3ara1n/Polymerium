using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI;
using Humanizer;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Views.Instances;
using Polymerium.Core;
using Polymerium.Core.Components;
using Polymerium.Core.Engines;
using Polymerium.Core.Extensions;
using Polymerium.Core.GameAssets;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly GameManager _gameManager;
    private readonly ResolveEngine _resolver;
    private readonly NavigationService _navigationService;
    private readonly IFileBaseService _fileBase;
    private readonly IOverlayService _overlayService;

    private string coreVersion = string.Empty;

    private bool isModSupported;
    private bool isShaderSupported;
    private uint resourcePackCount;
    private uint modCount;
    private uint shaderPackCount;

    private bool isRestorationNeeded;
    private Uri? referenceUrl;

    public InstanceViewModel(
        ViewModelContext context,
        InstanceManager instanceManager,
        ResolveEngine resolver,
        IOverlayService overlayService,
        IFileBaseService fileBase,
        ComponentManager componentManager,
        NavigationService navigationService,
        GameManager gameManager
    )
    {
        _overlayService = overlayService;
        _componentManager = componentManager;
        _resolver = resolver;
        _navigationService = navigationService;
        _gameManager = gameManager;
        _fileBase = fileBase;
        Context = context;
        CoreVersion = Context.AssociatedInstance!.Inner.GetCoreVersion() ?? "N/A";
        StartCommand = new RelayCommand(Start);
        OpenAssetDrawerCommand = new RelayCommand<string>(OpenAssetDrawer);
        GotoConfigurationViewCommand = new RelayCommand(GotoConfigurationView);
        Components = new ObservableCollection<ComponentTagItemModel>(
            BuildComponentModels(Context.AssociatedInstance!.Components)
        );
        InformationItems = new ObservableCollection<InstanceInformationItemModel>
        {
            new("\uF427", "标志符", Context.AssociatedInstance.Id),
            new(
                "\uE125",
                "作者",
                string.IsNullOrEmpty(Context.AssociatedInstance.Author)
                    ? "(未标注)"
                    : Context.AssociatedInstance.Author
            ),
            new("\uE121", "游戏时间", Context.AssociatedInstance.PlayTime.Humanize()),
            new(
                "\uEC92",
                "最近一次游玩",
                Context.AssociatedInstance.LastPlay == null
                    ? "从未"
                    : Context.AssociatedInstance.LastPlay.Humanize()
            ),
            new("\uEB50", "游玩次数", $"{Context.AssociatedInstance.PlayCount} 次"),
            new(
                "\uEB05",
                "启动成功率",
                Context.AssociatedInstance.PlayCount == 0
                    ? "N/A"
                    : $"{(Context.AssociatedInstance.PlayCount - Context.AssociatedInstance.ExceptionCount) / (float)Context.AssociatedInstance.PlayCount * 100}%"
            ),
            new("\uEC92", "创建时间", Context.AssociatedInstance.CreatedAt.Humanize())
            {
                Caption = "创建时间",
                IconGlyph = "\uEC92",
                Content = Context.AssociatedInstance.CreatedAt.Humanize()
            },
            new(
                "\uEC92",
                "最近一次还原",
                Context.AssociatedInstance.LastRestore == null
                    ? "从未"
                    : Context.AssociatedInstance.LastRestore.Humanize()
            )
        };
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
        IsModSupported = Context.AssociatedInstance.Components.Any(x => ComponentMeta.MINECRAFT != x.Identity);
        IsShaderSupported = true;
    }

    public string CoreVersion
    {
        get => coreVersion;
        set => SetProperty(ref coreVersion, value);
    }

    public ObservableCollection<ComponentTagItemModel> Components { get; }
    public ObservableCollection<InstanceInformationItemModel> InformationItems { get; }
    public ObservableCollection<AssetRaw> RawAssetSource { get; }
    public IAdvancedCollectionView RawShaderPacks { get; }
    public IAdvancedCollectionView RawMods { get; }
    public IAdvancedCollectionView RawResourcePacks { get; }

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

    public ViewModelContext Context { get; }
    public ICommand StartCommand { get; }
    public ICommand OpenAssetDrawerCommand { get; }
    public ICommand GotoConfigurationViewCommand { get; }

    public void Start()
    {
        var dialog = new PrepareGameDialog(Context.AssociatedInstance!.Inner, _overlayService);
        _overlayService.Show(dialog);
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

    public void OpenAssetDrawer(string? type)
    {
        var drawer = type switch
        {
            "ShaderPack" => new InstanceAssetDrawer(ResourceType.ShaderPack, RawShaderPacks),
            "Mod" => new InstanceAssetDrawer(ResourceType.Mod, RawMods),
            "ResourcePack" => new InstanceAssetDrawer(ResourceType.ResourcePack, RawResourcePacks),
            _ => throw new NotImplementedException()
        };
        drawer.OverlayService = _overlayService;
        _overlayService.Show(drawer);
    }

    public void GotoConfigurationView()
    {
        _navigationService.Navigate<InstanceConfigurationView>();
    }

    public void LoadAssets()
    {
        var assets = _gameManager.ScanAssets(Context.AssociatedInstance!.Inner);
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

    public async Task LoadInstanceInformationAsync(Action<Uri?, bool> callback)
    {
        var isNeeded = !Context.AssociatedInstance!.Inner.CheckIfRestored(_fileBase, out _);
        Uri? url = null;
        if (Context.AssociatedInstance.ReferenceSource != null)
        {
            var result = await _resolver.ResolveAsync(Context.AssociatedInstance!.ReferenceSource,
                Context.AssociatedInstance!.Inner);
            if (result) url = result.Value.Resource.Reference;
        }

        callback(url, isNeeded);
    }
}