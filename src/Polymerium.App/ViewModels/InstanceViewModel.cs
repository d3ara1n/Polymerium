using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using Polymerium.Core.Extensions;
using Polymerium.Core.GameAssets;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly GameManager _gameManager;
    private readonly NavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    private string coreVersion = string.Empty;
    private bool isModSupported;

    private bool isShaderSupported;

    public InstanceViewModel(
        ViewModelContext context,
        InstanceManager instanceManager,
        IOverlayService overlayService,
        ComponentManager componentManager,
        NavigationService navigationService,
        GameManager gameManager
    )
    {
        _overlayService = overlayService;
        _componentManager = componentManager;
        _navigationService = navigationService;
        _gameManager = gameManager;
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
        RawAssetSource = new ObservableCollection<AssetRaw>(gameManager.ScanAssets(Context.AssociatedInstance!.Inner));
        RawShaders = new AdvancedCollectionView
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
    public IAdvancedCollectionView RawShaders { get; }
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
            "ShaderPack" => new InstanceAssetDrawer(ResourceType.ShaderPack, RawShaders),
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
}