using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Engines;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class MetadataViewModel : ViewModelBase
{
    private readonly DialogService _dialogService;
    private readonly DispatcherQueue _dispatcher;
    private readonly ProfileManager _profileManager;
    private readonly IServiceProvider _provider;
    private readonly RepositoryAgent _repositoryAgent;

    private DataLoadingState attachmentLoadingState = DataLoadingState.Loading;

    private MetadataModel model;

    private LayerModel? selectedLayer;

    public MetadataViewModel(RepositoryAgent repositoryAgent, ProfileManager profileManager,
        DialogService dialogService, IServiceProvider provider)
    {
        _profileManager = profileManager;
        _repositoryAgent = repositoryAgent;
        _dialogService = dialogService;
        _provider = provider;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        model = new MetadataModel(ProfileManager.DUMMY_KEY, ProfileManager.DUMMY_PROFILE, dialogService);

        OpenAttachmentCommand = new RelayCommand<AttachmentModel>(OpenAttachment, CanOpenAttachment);
        RetryAttachmentCommand = new RelayCommand<AttachmentModel>(RetryAttachment);
        DeleteAttachmentCommand = new RelayCommand<AttachmentModel>(DeleteAttachment, CanDeleteAttachment);
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

                if (value != null) UpdateAttachmentSource(value);
            }
        }
    }

    public ObservableCollection<AttachmentModel> Attachments { get; } = new();

    public DataLoadingState AttachmentLoadingState
    {
        get => attachmentLoadingState;
        set => SetProperty(ref attachmentLoadingState, value);
    }

    private ICommand OpenAttachmentCommand { get; }
    private ICommand RetryAttachmentCommand { get; }
    private ICommand DeleteAttachmentCommand { get; }

    private void UpdateAttachmentSource(LayerModel layer)
    {
        AttachmentLoadingState = DataLoadingState.Loading;
        Task.Run(async () =>
        {
            var engine = _provider.GetRequiredService<ResolveEngine>();
            engine.SetFilter(Model.Inner.Metadata.ExtractFilter());
            foreach (var item in layer.Attachments)
                engine.AddAttachment(item);
            await foreach (var result in engine.WithCancellation(layer.Token).ConfigureAwait(false))
            {
                if (layer.Token.IsCancellationRequested) break;
                if (result is { IsResolvedSuccessfully: true, Result: not null })
                {
                    var package = result.Result;
                    var attachment = new AttachmentModel(result.Purl, layer, DataLoadingState.Done, package.ProjectName,
                        package.VersionName, package.Thumbnail, package.Summary, package.Reference, package.Kind,
                        OpenAttachmentCommand,
                        RetryAttachmentCommand,
                        DeleteAttachmentCommand);
                    _dispatcher.TryEnqueue(() => { Attachments.Add(attachment); });
                }
                else
                {
                    var attachment = new AttachmentModel(result.Purl, layer, DataLoadingState.Failed, null, null, null,
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
            var profile = _profileManager.GetProfile(key);
            if (profile != null)
                Model = new MetadataModel(key, profile, _dialogService);
            return profile != null;
        }

        return false;
    }

    public override void OnDetached()
    {
        if (Model.Key != ProfileManager.DUMMY_KEY) _profileManager.Flush(Model.Key);
    }

    public void AddLayer(string summary)
    {
        Model.AddLayer(new Metadata.Layer(null, true, summary, new List<Loader>(), new List<string>()));
    }

    private bool CanOpenAttachment(AttachmentModel? attachment)
    {
        return attachment != null && attachment.Reference.Value != null;
    }

    private void OpenAttachment(AttachmentModel? attachment)
    {
        if (attachment != null && attachment.Reference.Value != null)
            Process.Start(new ProcessStartInfo(attachment.Reference.Value.AbsoluteUri)
            {
                UseShellExecute = true
            });
    }

    private void RetryAttachment(AttachmentModel? attachment)
    {
        if (attachment != null)
            if (PurlHelper.TryParse(attachment.Inner, out var result) && result.HasValue)
            {
                attachment.State.Value = DataLoadingState.Loading;
                Task.Run(async () =>
                {
                    try
                    {
                        var package = await _repositoryAgent.ResolveAsync(result.Value.type, result.Value.name,
                            result.Value.version,
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
}