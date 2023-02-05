using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
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
        Context = context;

        StartCommand = new RelayCommand(Start);
        GotoConfigurationViewCommand = new RelayCommand(GotoConfigurationView);
        Components =
            new ObservableCollection<ComponentTagItemModel>(
                BuildComponentModels(Context.AssociatedInstance.Components));
        InformationItems = new ObservableCollection<InstanceInformationItemModel>
        {
            new()
            {
                Caption = "标志符",
                IconGlyph = "\uF427",
                Content = Context.AssociatedInstance.Id
            },
            new()
            {
                Caption = "作者",
                IconGlyph = "\uE125",
                Content = string.IsNullOrEmpty(Context.AssociatedInstance.Author)
                    ? "(未标注)"
                    : Context.AssociatedInstance.Author
            },
            new()
            {
                Caption = "游戏时间",
                IconGlyph = "\uE121",
                Content = Context.AssociatedInstance.PlayTime.Humanize()
            },
            new()
            {
                Caption = "最近一次游玩",
                IconGlyph = "\uEC92",
                Content = Context.AssociatedInstance.LastPlay == null
                    ? "从未"
                    : Context.AssociatedInstance.LastPlay.Humanize()
            },
            new()
            {
                Caption = "游玩次数",
                IconGlyph = "\uEB50",
                Content = $"{Context.AssociatedInstance.PlayCount} 次"
            },
            new()
            {
                Caption = "启动成功率",
                IconGlyph = "\uEB05",
                Content = Context.AssociatedInstance.PlayCount == 0
                    ? "N/A"
                    : $"{(Context.AssociatedInstance.PlayCount - Context.AssociatedInstance.ExceptionCount) / (float)Context.AssociatedInstance.PlayCount * 100}%"
            },
            new()
            {
                Caption = "创建时间",
                IconGlyph = "\uEC92",
                Content = Context.AssociatedInstance.CreatedAt.Humanize()
            },
            new()
            {
                Caption = "最近一次还原",
                IconGlyph = "\uEC92",
                Content = Context.AssociatedInstance.LastRestore == null
                    ? "从未"
                    : Context.AssociatedInstance.LastRestore.Humanize()
            }
        };
    }

    public ObservableCollection<ComponentTagItemModel> Components { get; }
    public ObservableCollection<InstanceInformationItemModel> InformationItems { get; }

    public ViewModelContext Context { get; }
    public ICommand StartCommand { get; }
    public ICommand GotoConfigurationViewCommand { get; }

    public void Start()
    {
        var dialog = new PrepareGameDialog(Context.AssociatedInstance.Inner, _overlayService);
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

    public void GotoConfigurationView()
    {
        _navigationService.Navigate<InstanceConfigurationView>();
    }
}