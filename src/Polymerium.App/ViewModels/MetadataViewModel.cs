using CommunityToolkit.Mvvm.Input;
using CsvHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Models.PrismLauncher;
using Polymerium.Trident.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Exceptions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;
using static Trident.Abstractions.Metadata;

namespace Polymerium.App.ViewModels;

public class MetadataViewModel : ViewModelBase
{
    private readonly DialogService _dialogService;
    private readonly DispatcherQueue _dispatcher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ModalService _modalService;
    private readonly NavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private readonly ProfileManager _profileManager;
    private readonly IServiceProvider _provider;
    private readonly RepositoryAgent _repositoryAgent;

    private DataLoadingState attachmentLoadingState = DataLoadingState.Loading;

    private MetadataModel model;

    private LayerModel? selectedLayer;

    public MetadataViewModel(RepositoryAgent repositoryAgent, ProfileManager profileManager,
        DialogService dialogService, IServiceProvider provider, NavigationService navigationService,
        ModalService modalService, IHttpClientFactory factory, NotificationService notificationService)
    {
        _profileManager = profileManager;
        _repositoryAgent = repositoryAgent;
        _provider = provider;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _modalService = modalService;
        _httpClientFactory = factory;
        _notificationService = notificationService;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        RenameLayerCommand = new RelayCommand<LayerModel>(RenameLayer, CanRenameLayer);
        UnlockLayerCommand = new RelayCommand<LayerModel>(UnlockLayer, CanUnlockLayer);
        UpdateLayerCommand = new RelayCommand<LayerModel>(UpdateLayer, CanUpdateLayer);
        DuplicateLayerCommand = new RelayCommand<LayerModel>(DuplicateLayer, CanDuplicateLayer);
        DeleteLayerCommand = new RelayCommand<LayerModel>(DeleteLayerAsync, CanDeleteLayer);
        OpenAttachmentCommand = new RelayCommand<AttachmentModel>(OpenAttachment, CanOpenAttachment);
        RetryAttachmentCommand = new RelayCommand<AttachmentModel>(RetryAttachment);
        ModifyAttachmentCommand = new RelayCommand<AttachmentModel>(ModifyAttachment, CanModifyAttachment);
        DeleteAttachmentCommand = new RelayCommand<AttachmentModel>(DeleteAttachment, CanDeleteAttachment);
        GotoWorkbenchViewCommand = new RelayCommand<bool>(GotoWorkbench, CanGotoWorkbench);
        OpenBulkUpdateModalCommand = new RelayCommand(OpenBulkUpdateModal, CanOpenBulkUpdateModal);
        ExportAttachmentListCommand =
            new AsyncRelayCommand<DataLoadingState>(ExportAttachmentListAsync, CanExportAttachmentList);

        model = new MetadataModel(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE, RenameLayerCommand,
            UnlockLayerCommand, UpdateLayerCommand, DuplicateLayerCommand, DeleteLayerCommand);
        _notificationService = notificationService;
    }

    public MetadataModel Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public LayerModel? SelectedLayer
    {
        get => selectedLayer;
        set
        {
            var old = selectedLayer;
            if (SetProperty(ref selectedLayer, value))
            {
                if (old != null)
                {
                    old.Discard();
                    Attachments.Clear();
                }

                if (value != null)
                {
                    UpdateAttachmentSource(value);
                }
            }
        }
    }

    public ObservableCollection<AttachmentModel> Attachments { get; } = new();

    public DataLoadingState AttachmentLoadingState
    {
        get => attachmentLoadingState;
        set => SetProperty(ref attachmentLoadingState, value);
    }

    public ICommand RenameLayerCommand { get; }
    public ICommand UnlockLayerCommand { get; }
    public ICommand UpdateLayerCommand { get; }
    public ICommand DuplicateLayerCommand { get; }
    public ICommand DeleteLayerCommand { get; }
    private ICommand OpenAttachmentCommand { get; }
    private ICommand RetryAttachmentCommand { get; }
    private ICommand ModifyAttachmentCommand { get; }
    private ICommand DeleteAttachmentCommand { get; }

    // Attachments 列表操作，都针对整个列表操作可以在这里直接访问 SelectedLayer 进行，就不代理到 LayerModel 里了。
    public ICommand GotoWorkbenchViewCommand { get; }
    public ICommand OpenBulkUpdateModalCommand { get; } = null!;
    public ICommand OpenChangelogModalCommand { get; } = null!;
    public ICommand ExportAttachmentListCommand { get; }

    private void UpdateAttachmentSource(LayerModel layer)
    {
        AttachmentLoadingState = DataLoadingState.Loading;
        Task.Run(async () =>
        {
            var engine = _provider.GetRequiredService<ResolveEngine>();
            engine.SetFilter(Model.Inner.Metadata.ExtractFilter());
            foreach (var item in layer.Attachments)
            {
                engine.AddAttachment(item);
            }

            await foreach (var result in engine.WithCancellation(layer.Token))
            {
                if (layer.Token.IsCancellationRequested)
                {
                    break;
                }

                if (result is { IsResolvedSuccessfully: true, Result: not null })
                {
                    var package = result.Result;
                    AttachmentModel attachment = new(result.Attachment, layer, DataLoadingState.Done,
                        package.ProjectName,
                        package.VersionName, package.Thumbnail, package.Summary, package.Reference, package.Kind,
                        OpenAttachmentCommand,
                        RetryAttachmentCommand,
                        ModifyAttachmentCommand,
                        DeleteAttachmentCommand);
                    _dispatcher.TryEnqueue(() => { Attachments.Add(attachment); });
                }
                else
                {
                    AttachmentModel attachment = new(result.Attachment, layer, DataLoadingState.Failed, null, null,
                        null,
                        null, null, null, OpenAttachmentCommand, RetryAttachmentCommand, ModifyAttachmentCommand,
                        DeleteAttachmentCommand);
                    _dispatcher.TryEnqueue(() => { Attachments.Add(attachment); });
                }
            }

            _dispatcher.TryEnqueue(() => { AttachmentLoadingState = DataLoadingState.Done; });
        });
    }

    public override bool OnAttached(object? maybeKey)
    {
        if (maybeKey is string key)
        {
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
            {
                Model = new MetadataModel(key, profile, RenameLayerCommand, UnlockLayerCommand, UpdateLayerCommand,
                    DuplicateLayerCommand,
                    DeleteLayerCommand);
            }

            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY)
        {
            _profileManager.Flush(Model.Key);
        }
    }

    public void AddLayer(string summary) =>
        Model.AddLayer(new Layer(null, true, summary, new List<Loader>(), new List<Attachment>()));

    private bool CanOpenAttachment(AttachmentModel? attachment) =>
        attachment != null && attachment.Reference.Value != null;

    private void OpenAttachment(AttachmentModel? attachment)
    {
        if (attachment != null && attachment.Reference.Value != null)
        {
            UriFileHelper.OpenInExternal(attachment.Reference.Value.AbsoluteUri);
        }
    }

    private void RetryAttachment(AttachmentModel? attachment)
    {
        if (attachment != null)
        {
            attachment.State.Value = DataLoadingState.Loading;
            Task.Run(async () =>
            {
                try
                {
                    var package = await _repositoryAgent.ResolveAsync(attachment.Inner.Label,
                        attachment.Inner.ProjectId,
                        attachment.Inner.VersionId,
                        Model.Inner.Metadata.ExtractFilter(), CancellationToken.None);
                    _dispatcher.TryEnqueue(() =>
                    {
                        attachment.State.Value = DataLoadingState.Done;
                        attachment.ProjectName.Value = package.ProjectName;
                        attachment.VersionName.Value = package.VersionName;
                        attachment.Thumbnail.Value = package.Thumbnail;
                        attachment.Reference.Value = package.Reference;
                        attachment.Summary.Value = package.Summary;
                        attachment.Kind.Value = package.Kind;
                    });
                }
                catch
                {
                    _dispatcher.TryEnqueue(() => attachment.State.Value = DataLoadingState.Failed);
                }
            });
        }
    }

    private bool CanModifyAttachment(AttachmentModel? attachment) => SelectedLayer is { IsLocked.Value: false };

    private void ModifyAttachment(AttachmentModel? attachment)
    {
        if (attachment != null && SelectedLayer != null)
        {
            ProjectPreviewModal modal = new(_repositoryAgent, attachment.Inner.Label, attachment.Inner.ProjectId,
                Model.Inner.Metadata.ExtractFilter() ?? Filter.EMPTY, SelectedLayer, it =>
                {
                    var found = Attachments.FirstOrDefault(x => x.Inner.ProjectId == it.Root.Inner.Id);
                    if (found != null)
                    {
                        found.VersionName.Value = it.Inner.Name;
                    }
                });
            _modalService.Pop(modal);
        }
    }

    private bool CanDeleteAttachment(AttachmentModel? attachment) => SelectedLayer is { IsLocked.Value: false };

    private void DeleteAttachment(AttachmentModel? attachment)
    {
        if (attachment != null && SelectedLayer != null)
        {
            Attachments.Remove(attachment);
            SelectedLayer.Attachments.Remove(attachment.Inner);
        }
    }

    private bool CanGotoWorkbench(bool locked) => locked is not true;


    private void GotoWorkbench(bool locked)
    {
        if (locked is not true && SelectedLayer != null)
        {
            _navigationService.Navigate(typeof(WorkbenchView), SelectedLayer);
        }
    }

    private bool CanRenameLayer(LayerModel? layer) => layer != null;

    private async void RenameLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var summary =
                await _dialogService.RequestTextAsync("Summarize usage of your new layer", layer.Summary.Value);
            if (summary != null)
            {
                layer.Summary.Value = summary;
            }
        }
    }

    private bool CanUnlockLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            return layer.IsLocked.Value;
        }

        return false;
    }

    private async void UnlockLayer(LayerModel? layer)
    {
        if (layer != null)
        {
            var confirmation = await _dialogService.RequestConfirmationAsync(
                "Unlocking a tagged layer will remove its tag and losing the ability to update metadata. Continue?");
            if (confirmation)
            {
                layer.IsLocked.Value = false;
            }
        }
    }

    private bool CanUpdateLayer(LayerModel? layer) => layer?.IsLocked.Value ?? false;

    private void UpdateLayer(LayerModel? layer)
    {
        if (layer != null)
            // TODO: pop UpdateLayerModal
        {
        }
    }

    private bool CanDeleteLayer(LayerModel? layer) => layer != null;

    private async void DeleteLayerAsync(LayerModel? layer)
    {
        if (layer != null)
        {
            var confirmation = await _dialogService.RequestConfirmationAsync(
                "This operation cannot be revoked. Continue?");
            if (confirmation)
            {
                Model.Layers.Remove(layer);
                Model.NotifyPositionChange();
            }
        }
    }

    private bool CanDuplicateLayer(LayerModel? layer) => layer != null;

    private void DuplicateLayer(LayerModel? original)
    {
        if (original != null)
        {
            var created = new Layer(null, false, $"{original.Inner.Summary} - Duplicated",
                new List<Loader>(original.Inner.Loaders), new List<Attachment>(original.Inner.Attachments));
            var model = new LayerModel(created, Model);

            Model.Layers.Add(model);
        }
    }

    private bool CanOpenBulkUpdateModal() => SelectedLayer is not null;

    private void OpenBulkUpdateModal()
    {
        if (SelectedLayer is not null)
        {
            var modal = new AttachmentBulkUpdateModal();
            _modalService.Pop(modal);
        }
    }

    public async Task<IEnumerable<LoaderVersionModel>> GetLoaderVersionsAsync(string identity)
    {
        var uid = identity switch
        {
            Loader.COMPONENT_FORGE => PrismLauncherHelper.UID_FORGE,
            Loader.COMPONENT_NEOFORGE => PrismLauncherHelper.UID_NEOFORGE,
            Loader.COMPONENT_FABRIC => PrismLauncherHelper.UID_FABRIC,
            Loader.COMPONENT_QUILT => PrismLauncherHelper.UID_QUILT,
            _ => throw new ResourceIdentityUnrecognizedException(identity, nameof(Loader))
        };
        var manifesta = await PrismLauncherHelper.GetManifestAsync(uid, _httpClientFactory);
        return manifesta.Versions.Where(x => x.Requires.Any(y =>
            y.Uid == PrismLauncherHelper.UID_INTERMEDIARY || (y.Uid == PrismLauncherHelper.UID_MINECRAFT &&
                                                              (y.Equal == model.Inner.Metadata.Version ||
                                                               y.Suggest == model.Inner.Metadata.Version)))).Select(x =>
            new LoaderVersionModel(identity, x.Version, x.ReleaseTime, x.Type switch
            {
                PrismReleaseType.Release => ReleaseType.Release,
                PrismReleaseType.Snapshot => ReleaseType.Snapshot,
                PrismReleaseType.Old_Snapshot => ReleaseType.Snapshot,
                PrismReleaseType.Experiment => ReleaseType.Experiment,
                PrismReleaseType.Old_Alpha => ReleaseType.Alpha,
                PrismReleaseType.Old_Beta => ReleaseType.Beta,
                _ => throw new NotImplementedException()
            }, x.Recommended));
    }

    private bool CanExportAttachmentList(DataLoadingState state) => state == DataLoadingState.Done;

    private async Task ExportAttachmentListAsync(DataLoadingState state)
    {
        var path = await _dialogService.RequsetSavePathAsync(
            FileNameHelper.Sanitize($"export_{Model.Inner.Name}_{SelectedLayer?.Inner.Summary ?? ""}.txt"));
        // 取消操作
        if (path is null) return;
        if (!File.Exists(path))
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (dir is not null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using var stream = File.OpenWrite(path);
                using var writer = new StreamWriter(stream);

                var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(Attachments.Select((x, i) => new
                {
                    Index = i,
                    x.Inner.Label,
                    x.Inner.ProjectId,
                    x.Inner.VersionId,
                    ProjectName = x.ProjectName.Value.Replace(",", string.Empty),
                    VersionName = x.VersionName.Value.Replace(",", string.Empty)
                }));
                _notificationService.PopSuccess($"Exported to {path}");
            }
            catch (Exception e)
            {
                _notificationService.PopError(e.Message);
            }
        }
        else
        {
            _notificationService.PopError("File path is not valid or already exist");
        }
    }
}