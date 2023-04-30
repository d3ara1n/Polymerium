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
using DotNext;
using Polymerium.Abstractions.Meta;
using Polymerium.Abstractions.ResourceResolving;
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
    private readonly ResolveEngine _resolver;

    private Action<InstanceAttachmentItemModel?>? addAttachmentCallback;

    public InstanceMetadataConfigurationViewModel(
        ViewModelContext context,
        ComponentManager componentManager,
        IOverlayService overlayService,
        ResolveEngine resolver,
        NavigationService navigation
    )
    {
        Instance = context.AssociatedInstance!;
        OverlayService = overlayService;
        _componentManager = componentManager;
        _resolver = resolver;
        _navigationService = navigation;
        AddComponentCommand = new RelayCommand(AddComponent);
        GotoSearchCenterCommand = new RelayCommand(GotoSearchCenter);
        OpenReferenceUrlCommand = new RelayCommand<Uri>(OpenReferenceUrl);
        RemoveAttachmentSelfCommand = new RelayCommand<InstanceAttachmentItemModel>(
            RemoveAttachmentSelf
        );
        RemoveComponentSelfCommand = new RelayCommand<InstanceComponentItemModel>(
            RemoveComponentSelf
        );
        Components = new ObservableCollection<InstanceComponentItemModel>(
            Instance.Components.Select(FromComponent)
                ?? Enumerable.Empty<InstanceComponentItemModel>()
        );
        Attachments = new ObservableCollection<InstanceAttachmentItemModel>();
        Instance.Components.CollectionChanged += Components_OnCollectionChanged;
        Instance.Attachments.CollectionChanged += Attachments_CollectionChanged;
    }

    public IOverlayService OverlayService { get; }
    public GameInstanceModel Instance { get; }
    public ObservableCollection<InstanceComponentItemModel> Components { get; }
    public ObservableCollection<InstanceAttachmentItemModel> Attachments { get; }
    public ICommand AddComponentCommand { get; }
    public ICommand GotoSearchCenterCommand { get; }
    public ICommand OpenReferenceUrlCommand { get; }
    public ICommand RemoveAttachmentSelfCommand { get; }
    public IRelayCommand<InstanceComponentItemModel> RemoveComponentSelfCommand { get; }

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
                    Task.Run(() => LoadParseAttachmentsAsync(e.NewItems.Cast<Attachment>()));
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    var collection = Attachments
                        .Where(x => e.OldItems.Contains(x.Attachment))
                        .ToList();
                    foreach (var model in collection)
                        Attachments.Remove(model);
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

    public async Task LoadParseReferenceAsync(Action<InstanceModpackReferenceModel?> callback)
    {
        var reference = Instance.ReferenceSource!;
        var context = new ResolverContext(Instance.Inner);
        var modpackResult = await _resolver.ResolveAsync(reference, context);
        var updateResult = await _resolver.ResolveToUpdateAsync(reference, context);
        if (modpackResult.IsSuccessful && modpackResult.Value.Type == ResourceType.Modpack)
        {
            if (
                updateResult.IsSuccessful
                && updateResult.Value.Type == ResourceType.Update
                && updateResult.Value.Resource is Update update
            )
            {
                var versionTasks = update.Versions.Select(
                    x => (x, _resolver.ResolveAsync(x, context))
                );
                await Task.WhenAll(versionTasks.Select(x => x.Item2));
                if (
                    versionTasks.All(
                        x => x.Item2.IsCompletedSuccessfully && x.Item2.Result.IsSuccessful
                    )
                )
                {
                    var versions = versionTasks.Select(
                        x =>
                            new InstanceModpackReferenceVersionModel(
                                x.Item2.Result.Value.Resource.Id,
                                x.Item2.Result.Value.Resource.VersionId,
                                x.Item2.Result.Value.Resource.Version,
                                x.Item2.Result.Value.Resource.VersionId
                                    == modpackResult.Value.Resource.VersionId,
                                x.Item1
                            )
                    );
                    var model = new InstanceModpackReferenceModel(
                        modpackResult.Value.Resource.Name,
                        modpackResult.Value.Resource.Id,
                        modpackResult.Value.Resource.Version,
                        modpackResult.Value.Resource.VersionId,
                        modpackResult.Value.Resource.Author,
                        modpackResult.Value.Resource.Summary,
                        versions,
                        reference
                    );
                    callback(model);
                }
                else
                    callback(null);
            }
            else
                callback(null);
        }
        else
            callback(null);
    }

    public async Task LoadParseAttachmentsAsync(IEnumerable<Attachment> newlyAdded)
    {
        var context = new ResolverContext(Instance.Inner);
        var tasks = new List<Task>();
        foreach (var attachment in newlyAdded)
            tasks.Add(LoadAddAttachmentInfoAsync(attachment, context, addAttachmentCallback!));
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
            !Instance.IsTagged,
            RemoveComponentSelfCommand
        );
    }

    private async Task LoadAddAttachmentInfoAsync(
        Attachment attachment,
        ResolverContext context,
        Action<InstanceAttachmentItemModel?> callback
    )
    {
        var result = await _resolver.ResolveAsync(attachment.Source, context);
        InstanceAttachmentItemModel model = null!;
        var isLocked =
            Instance.ReferenceSource != null && attachment.From == Instance.ReferenceSource;
        if (result.IsSuccessful)
        {
            var item = result.Value;
            model = new InstanceAttachmentItemModel(
                item.Type,
                item.Resource.Name,
                item.Resource.Author,
                item.Resource.IconSource,
                item.Resource.Reference,
                item.Resource.Version,
                item.Resource.Summary,
                attachment,
                isLocked,
                OpenReferenceUrlCommand,
                RemoveAttachmentSelfCommand
            );
        }
        else
        {
            model = new InstanceAttachmentItemModel(
                ResourceType.None,
                "未知",
                "未知",
                null,
                null,
                "N/A",
                attachment.Source.AbsoluteUri,
                attachment,
                isLocked,
                OpenReferenceUrlCommand,
                RemoveAttachmentSelfCommand
            );
        }

        callback(model);
    }

    private void GotoSearchCenter()
    {
        _navigationService.Navigate<SearchCenterView>();
    }

    private void AddComponent()
    {
        var dialog = new AddMetaComponentWizardDialog { OverlayService = OverlayService };
        OverlayService.Show(dialog);
    }

    private void RemoveComponentSelf(InstanceComponentItemModel? model)
    {
        if (Instance.Components.Any(x => x.Identity == model?.Id && x.Version == model.Version))
        {
            var item = Instance.Components.First(
                x => x.Identity == model?.Id && x.Version == model.Version
            );
            Instance.Components.Remove(item);
        }
    }

    private void RemoveAttachmentSelf(InstanceAttachmentItemModel? model)
    {
        if (model != null && Instance.Attachments.Contains(model.Attachment))
            Instance.Attachments.Remove(model.Attachment);
    }

    private void OpenReferenceUrl(Uri? reference)
    {
        if (reference != null)
            Process.Start(new ProcessStartInfo(reference.AbsoluteUri) { UseShellExecute = true });
    }

    public void Unlock()
    {
        Instance.ReferenceSource = null;
    }
}
