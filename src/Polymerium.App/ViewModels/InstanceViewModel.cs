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
using Polymerium.Core.Extensions;

namespace Polymerium.App.ViewModels;

public class InstanceViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly InstanceManager _instanceManager;
    private readonly NavigationService _navigationService;
    private readonly IOverlayService _overlayService;

    public InstanceViewModel(
        InstanceManager instanceManager,
        IOverlayService overlayService,
        ComponentManager componentManager,
        NavigationService navigationService,
        ViewModelContext context
    )
    {
        _instanceManager = instanceManager;
        _overlayService = overlayService;
        _componentManager = componentManager;
        _navigationService = navigationService;
        Context = context;
        CoreVersion = Context.AssociatedInstance.Inner.GetCoreVersion() ?? "N/A";
        StartCommand = new RelayCommand(Start);
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
    }

    private string coreVersion = string.Empty;

    public string CoreVersion
    {
        get => coreVersion;
        set => SetProperty(ref coreVersion, value);
    }

    public ObservableCollection<ComponentTagItemModel> Components { get; }
    public ObservableCollection<InstanceInformationItemModel> InformationItems { get; }

    public ViewModelContext Context { get; }
    public ICommand StartCommand { get; }
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

    public void GotoConfigurationView()
    {
        _navigationService.Navigate<InstanceConfigurationView>();
    }
}
