using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.Resources;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Core.Engines;

namespace Polymerium.App.ViewModels.Instances;

public class InstanceMetadataConfigurationViewModel : ObservableObject
{
    private readonly ComponentManager _componentManager;
    private readonly NavigationService _navigationService;
    private readonly IOverlayService _overlayService;
    private readonly ResolveEngine _resolver;

    private Action<InstanceAttachmentItemModel?>? addAttachmentCallback;
    private bool isAttachmentBeingParsed = true;

    public InstanceMetadataConfigurationViewModel(
        ViewModelContext context,
        ComponentManager componentManager,
        IOverlayService overlayService,
        ResolveEngine resolver,
        NavigationService navigation
    )
    {
        Context = context;
        _componentManager = componentManager;
        _overlayService = overlayService;
        _resolver = resolver;
        _navigationService = navigation;
        AddComponentCommand = new RelayCommand(AddComponent);
        GotoSearchCenterCommand = new RelayCommand(GotoSearchCenter);
        OpenReferenceUrlCommand = new RelayCommand<Uri>(OpenReferenceUrl);
        RemoveComponentSelfCommand = new RelayCommand<InstanceComponentItemModel>(RemoveComponentSelf);
        RemoveAttachmentsCommand = new RelayCommand<IList<object>>(RemoveAttachments);
        Components = new ObservableCollection<InstanceComponentItemModel>(
            Context.AssociatedInstance?.Components.Select(FromComponent)
            ?? Enumerable.Empty<InstanceComponentItemModel>()
        );
        Attachments = new ObservableCollection<InstanceAttachmentItemModel>();
        Context.AssociatedInstance!.Components.CollectionChanged += Components_OnCollectionChanged;
        Context.AssociatedInstance!.Attachments.CollectionChanged += Attachments_CollectionChanged;
    }

    public ViewModelContext Context { get; }
    public ObservableCollection<InstanceComponentItemModel> Components { get; }
    public ObservableCollection<InstanceAttachmentItemModel> Attachments { get; }
    public ICommand AddComponentCommand { get; }
    public ICommand GotoSearchCenterCommand { get; }
    public ICommand OpenReferenceUrlCommand { get; }
    public IRelayCommand<InstanceComponentItemModel> RemoveComponentSelfCommand { get; }
    public IRelayCommand<IList<object>> RemoveAttachmentsCommand { get; }

    public bool IsAttachmentBeingParsed
    {
        get => isAttachmentBeingParsed;
        set => SetProperty(ref isAttachmentBeingParsed, value);
    }

    public void SetCallback(Action<InstanceAttachmentItemModel?> callback)
    {
        addAttachmentCallback = callback;
    }

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

    private void Attachments_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null && e.NewItems.Count > 0)
                    Task.Run(() => LoadParseAttachmentsAsync((IEnumerable<Uri>)e.NewItems));
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    var collection = Attachments.Where(x => e.OldItems.Contains(x.Attachment)).ToList();
                    foreach (var model in collection) Attachments.Remove(model);
                }

                break;
            case NotifyCollectionChangedAction.Reset:
                Attachments.Clear();
                break;
            case NotifyCollectionChangedAction.Replace:
                // 如果未来要实现原地编辑并更新一个附件而不是移除后添加，就会有替换事件。
                throw new NotImplementedException();
        }
    }

    public async Task LoadParseAttachmentsAsync(IEnumerable<Uri> newlyAdded)
    {
        var tasks = new List<Task>();
        foreach (var attachment in newlyAdded)
            tasks.Add(LoadAddAttachmentInfoAsync(attachment, addAttachmentCallback!));
        await Task.WhenAll(tasks);
        addAttachmentCallback!(null);
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

    private async Task LoadAddAttachmentInfoAsync(Uri attachment, Action<InstanceAttachmentItemModel?> callback)
    {
        var result = await _resolver.ResolveAsync(attachment, Context.AssociatedInstance?.Inner);
        InstanceAttachmentItemModel model = null!;
        if (result.IsSuccessful)
        {
            var item = result.Value;
            model = new InstanceAttachmentItemModel(item.Type, item.Resource.Name, item.Resource.Author,
                item.Resource.IconSource, item.Resource.Reference, item.Resource.Version, item.Resource.Summary,
                attachment, OpenReferenceUrlCommand);
        }
        else
        {
            model = new InstanceAttachmentItemModel(ResourceType.None, "未知", "未知", null, null, "N/A",
                attachment.AbsoluteUri,
                attachment, OpenReferenceUrlCommand);
        }

        callback(model);
    }

    private void RemoveAttachments(IList<object>? items)
    {
        if (items != null)
            foreach (InstanceAttachmentItemModel item in items.ToArray())
                Context.AssociatedInstance!.Attachments.Remove(item.Attachment);
    }

    private void GotoSearchCenter()
    {
        _navigationService.Navigate<SearchCenterView>();
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

    private void OpenReferenceUrl(Uri? reference)
    {
        if (reference != null) Process.Start(new ProcessStartInfo(reference.AbsoluteUri) { UseShellExecute = true });
    }
}