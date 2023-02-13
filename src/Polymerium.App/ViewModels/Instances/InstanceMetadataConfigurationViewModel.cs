using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions.Meta;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceMetadataConfigurationViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly IOverlayService _overlayService;

    public InstanceMetadataConfigurationViewModel(
        ViewModelContext context,
        ComponentManager componentManager,
        IOverlayService overlayService
    )
    {
        Context = context;
        _componentManager = componentManager;
        _overlayService = overlayService;
        AddComponentCommand = new RelayCommand(AddComponent);
        RemoveComponentSelfCommand = new RelayCommand<InstanceComponentItemModel>(
            RemoveComponentSelf
        );
        Components = new ObservableCollection<InstanceComponentItemModel>(
            Context.AssociatedInstance?.Components.Select(FromComponent)
            ?? Enumerable.Empty<InstanceComponentItemModel>()
        );
        Context.AssociatedInstance!.Components.CollectionChanged += Components_OnCollectionChanged;
    }

    public ViewModelContext Context { get; }

    public ObservableCollection<InstanceComponentItemModel> Components { get; }

    public ICommand AddComponentCommand { get; }
    public IRelayCommand<InstanceComponentItemModel> RemoveComponentSelfCommand { get; }

    private void Components_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                    foreach (Component item in e.NewItems)
                        Components.Add(FromComponent(item));

                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                    foreach (Component item in e.OldItems)
                    {
                        var com = Components.FirstOrDefault(
                            x => x.Id == item.Identity && x.Version == item.Version
                        );
                        if (com != null)
                            Components.Remove(com);
                    }

                break;
            case NotifyCollectionChangedAction.Reset:
                Components.Clear();
                break;
        }
    }

    private InstanceComponentItemModel FromComponent(Component component)
    {
        _componentManager.TryFindByIdentity(component.Identity, out var meta);
        return new InstanceComponentItemModel(
            component.Identity,
            $"ms-appx:///Assets/Icons/GameComponents/{component.Identity}.png",
            meta?.FriendlyName ?? component.Identity,
            component.Version,
            RemoveComponentSelfCommand
        );
    }

    private void AddComponent()
    {
        var dialog = new AddMetaComponentWizardDialog { OverlayService = _overlayService };
        _overlayService.Show(dialog);
    }

    private void RemoveComponentSelf(InstanceComponentItemModel? model)
    {
        if (
            Context.AssociatedInstance!.Components.Any(
                x => x.Identity == model?.Id && x.Version == model.Version
            )
        )
        {
            var item = Context.AssociatedInstance.Components.First(
                x => x.Identity == model?.Id && x.Version == model.Version
            );
            Context.AssociatedInstance.Components.Remove(item);
        }
    }
}
