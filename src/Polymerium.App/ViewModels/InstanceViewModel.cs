using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Views.Instances;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : ObservableObject
{
    private readonly InstanceManager _instanceManager;
    private readonly IOverlayService _overlayService;
    private readonly ComponentManager _componentManager;
    private readonly NavigationService _navigationService;

    private GameInstance instance;

    public InstanceViewModel(InstanceManager instanceManager, IOverlayService overlayService,
        ComponentManager componentManager, NavigationService navigationService)
    {
        _instanceManager = instanceManager;
        _overlayService = overlayService;
        _componentManager = componentManager;
        _navigationService = navigationService;
    }

    public GameInstance Instance { get; set; }
    public ObservableCollection<ComponentTagItemModel> Components { get; set; }

    public void GotInstance(GameInstance instance)
    {
        Instance = instance;
        Components =
            new ObservableCollection<ComponentTagItemModel>(BuildComponentModels(instance.Metadata.Components));
    }

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
        _navigationService.Navigate<InstanceConfigurationView>();
    }
}