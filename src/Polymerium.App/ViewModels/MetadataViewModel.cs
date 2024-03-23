using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Engines.Resolving;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions;
using Trident.Abstractions.Resources;
using static Trident.Abstractions.Metadata;

namespace Polymerium.App.ViewModels
{
    public class MetadataViewModel : ViewModelBase
    {
        private readonly DialogService _dialogService;
        private readonly DispatcherQueue _dispatcher;
        private readonly NavigationService _navigationService;
        private readonly ProfileManager _profileManager;
        private readonly IServiceProvider _provider;
        private readonly RepositoryAgent _repositoryAgent;

        private DataLoadingState attachmentLoadingState = DataLoadingState.Loading;

        private MetadataModel model;

        private LayerModel? selectedLayer;

        public MetadataViewModel(RepositoryAgent repositoryAgent, ProfileManager profileManager,
            DialogService dialogService, IServiceProvider provider, NavigationService navigationService)
        {
            _profileManager = profileManager;
            _repositoryAgent = repositoryAgent;
            _provider = provider;
            _dialogService = dialogService;
            _navigationService = navigationService;
            _dispatcher = DispatcherQueue.GetForCurrentThread();

            RenameLayerCommand = new RelayCommand<LayerModel>(RenameLayer, CanRenameLayer);
            UnlockLayerCommand = new RelayCommand<LayerModel>(UnlockLayer, CanUnlockLayer);
            UpdateLayerCommand = new RelayCommand<LayerModel>(UpdateLayer, CanUpdateLayer);
            DeleteLayerCommand = new RelayCommand<LayerModel>(DeleteLayer, CanDeleteLayer);
            OpenAttachmentCommand = new RelayCommand<AttachmentModel>(OpenAttachment, CanOpenAttachment);
            RetryAttachmentCommand = new RelayCommand<AttachmentModel>(RetryAttachment);
            DeleteAttachmentCommand = new RelayCommand<AttachmentModel>(DeleteAttachment, CanDeleteAttachment);
            GotoWorkbenchViewCommand = new RelayCommand<bool>(GotoWorkbench, CanGotoWorkbench);

            model = new MetadataModel(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE, RenameLayerCommand,
                UnlockLayerCommand, UpdateLayerCommand, DeleteLayerCommand);
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
                LayerModel? old = selectedLayer;
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
        public ICommand DeleteLayerCommand { get; }
        private ICommand OpenAttachmentCommand { get; }
        private ICommand RetryAttachmentCommand { get; }
        private ICommand DeleteAttachmentCommand { get; }
        public ICommand GotoWorkbenchViewCommand { get; }

        private void UpdateAttachmentSource(LayerModel layer)
        {
            AttachmentLoadingState = DataLoadingState.Loading;
            Task.Run(async () =>
            {
                ResolveEngine engine = _provider.GetRequiredService<ResolveEngine>();
                engine.SetFilter(Model.Inner.Metadata.ExtractFilter());
                foreach (Attachment item in layer.Attachments)
                {
                    engine.AddAttachment(item);
                }

                await foreach (ResolveResult result in engine.WithCancellation(layer.Token))
                {
                    if (layer.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (result is { IsResolvedSuccessfully: true, Result: not null })
                    {
                        Package? package = result.Result;
                        AttachmentModel attachment = new(result.Attachment, layer, DataLoadingState.Done,
                            package.ProjectName,
                            package.VersionName, package.Thumbnail, package.Summary, package.Reference, package.Kind,
                            OpenAttachmentCommand,
                            RetryAttachmentCommand,
                            DeleteAttachmentCommand);
                        _dispatcher.TryEnqueue(() => { Attachments.Add(attachment); });
                    }
                    else
                    {
                        AttachmentModel attachment = new(result.Attachment, layer, DataLoadingState.Failed, null, null,
                            null,
                            null, null, null, OpenAttachmentCommand, RetryAttachmentCommand, DeleteAttachmentCommand);
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
                Profile? profile = _profileManager.GetProfile(key);
                if (profile != null)
                {
                    Model = new MetadataModel(key, profile, RenameLayerCommand, UnlockLayerCommand, UpdateLayerCommand,
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

        public void AddLayer(string summary)
        {
            Model.AddLayer(new Layer(null, true, summary, new List<Loader>(), new List<Attachment>()));
        }

        private bool CanOpenAttachment(AttachmentModel? attachment)
        {
            return attachment != null && attachment.Reference.Value != null;
        }

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
                        Package package = await _repositoryAgent.ResolveAsync(attachment.Inner.Label,
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

        private bool CanDeleteAttachment(AttachmentModel? attachment)
        {
            return SelectedLayer is { IsLocked.Value: false };
        }

        private void DeleteAttachment(AttachmentModel? attachment)
        {
            if (attachment != null && SelectedLayer != null)
            {
                // TODO: 将数据标记为删除，在离开页面时进行删除
                Attachments.Remove(attachment);
                SelectedLayer.Attachments.Remove(attachment.Inner);
            }
        }

        private bool CanGotoWorkbench(bool locked)
        {
            return locked is not true;
        }


        private void GotoWorkbench(bool locked)
        {
            if (locked is not true && SelectedLayer != null)
            {
                _navigationService.Navigate(typeof(WorkbenchView), SelectedLayer);
            }
        }

        private bool CanRenameLayer(LayerModel? layer)
        {
            return layer != null;
        }

        private async void RenameLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                string? summary =
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
                bool confirmation = await _dialogService.RequestConfirmationAsync(
                    "Unlocking a tagged layer will remove its tag and losing the ability to update metadata. Continue?");
                if (confirmation)
                {
                    layer.IsLocked.Value = false;
                }
            }
        }

        private bool CanUpdateLayer(LayerModel? layer)
        {
            return layer?.IsLocked.Value ?? false;
        }

        private void UpdateLayer(LayerModel? layer)
        {
            if (layer != null)
            // TODO: pop UpdateLayerModal
            {
            }
        }

        private bool CanDeleteLayer(LayerModel? layer)
        {
            return layer != null;
        }

        private async void DeleteLayer(LayerModel? layer)
        {
            if (layer != null)
            {
                bool confirmation = await _dialogService.RequestConfirmationAsync(
                    "This operation cannot be revoked. Continue?");
                if (confirmation)
                {
                    Model.Layers.Remove(layer);
                    Model.NotifyPositionChange();
                }
            }
        }
    }
}