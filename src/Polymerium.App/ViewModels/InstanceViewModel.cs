using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly ViewModelContext _context;
    private readonly InstanceManager _instanceManager;
    private readonly NavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    public InstanceViewModel(InstanceManager instanceManager, IOverlayService overlayService,
        ComponentManager componentManager, NavigationService navigationService, ViewModelContext context)
    {
        _instanceManager = instanceManager;
        _overlayService = overlayService;
        _componentManager = componentManager;
        _navigationService = navigationService;
        _context = context;

        Instance = _context.AssociatedInstance;
        Components =
            new ObservableCollection<ComponentTagItemModel>(BuildComponentModels(Instance.Metadata.Components));
        InformationItems = new ObservableCollection<InstanceInformationItemModel>
        {
            new()
            {
                Caption = "标志符",
                IconGlyph = "\uF427",
                Content = Instance.Id
            },
            new()
            {
                Caption = "作者",
                IconGlyph = "\uE125",
                Content = string.IsNullOrEmpty(Instance.Author) ? "(未标注)" : Instance.Author
            },
            new()
            {
                Caption = "游戏时间",
                IconGlyph = "\uE121",
                Content = Instance.PlayTime.Humanize()
            },
            new()
            {
                Caption = "最近一次游玩",
                IconGlyph = "\uEC92",
                Content = Instance.LastPlay == null ? "从未" : Instance.LastPlay.Humanize()
            },
            new()
            {
                Caption = "游玩次数",
                IconGlyph = "\uEB50",
                Content = $"{Instance.PlayCount} 次"
            },
            new()
            {
                Caption = "启动成功率",
                IconGlyph = "\uEB05",
                Content = Instance.PlayCount == 0
                    ? "N/A"
                    : $"{(Instance.PlayCount - Instance.ExceptionCount) / (float)Instance.PlayCount * 100}%"
            },
            new()
            {
                Caption = "创建时间",
                IconGlyph = "\uEC92",
                Content = Instance.CreatedAt.Humanize()
            },
            new()
            {
                Caption = "最近一次还原",
                IconGlyph = "\uEC92",
                Content = Instance.LastRestore == null ? "从未" : Instance.LastRestore.Humanize()
            }
        };
    }

    public GameInstance Instance { get; }
    public ObservableCollection<ComponentTagItemModel> Components { get; }
    public ObservableCollection<InstanceInformationItemModel> InformationItems { get; }

    [RelayCommand]
    public void Start()
    {
        var dialog = new PrepareGameDialog(Instance, _overlayService);
        _overlayService.Show(dialog);
    }

    private IEnumerable<ComponentTagItemModel> BuildComponentModels(IEnumerable<Component> components)
    {
        return components.Select(
            x =>
            {
                _componentManager.TryFindByIdentity(x.Identity, out var meta);
                return new ComponentTagItemModel
                {
                    Identity = x.Identity,
                    Name = meta?.FriendlyName ?? x.Identity,
                    Version = x.Version,
                    Description = $"{x.Identity}:{x.Version}"
                };
            });
    }

    [RelayCommand]
    public void GotoConfigurationView()
    {
        _navigationService.Navigate<InstanceConfigurationView>(Instance);
    }
}