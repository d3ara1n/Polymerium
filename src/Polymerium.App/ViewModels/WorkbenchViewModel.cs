using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class WorkbenchViewModel : ViewModelBase
{
    private readonly ModalService _modalService;
    private readonly RepositoryAgent _repositoryAgent;
    private readonly ThumbnailSaver _thumbnailSaver;
    private string? background;
    private Filter baseFilter = Filter.EMPTY;
    private LayerModel? model;

    private IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? results;

    public WorkbenchViewModel(ThumbnailSaver thumbnailSaver, RepositoryAgent repositoryAgent, ModalService modalService)
    {
        _thumbnailSaver = thumbnailSaver;
        _modalService = modalService;
        _repositoryAgent = repositoryAgent;

        OpenResourceModalCommand = new RelayCommand<ExhibitModel>(OpenResourceModal);
        InstallAttachmentCommand = new RelayCommand<ModpackModel>(InstallAttachment);
        UninstallAttachmentCommand = new RelayCommand<ModpackModel>(UninstallAttachment);
    }

    public LayerModel? Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public string? Background
    {
        get => background;
        set => SetProperty(ref background, value);
    }

    public IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? Results
    {
        get => results;
        set => SetProperty(ref results, value);
    }

    public ICommand OpenResourceModalCommand { get; }
    private ICommand InstallAttachmentCommand { get; }
    private ICommand UninstallAttachmentCommand { get; }

    public override bool OnAttached(object? maybeLayer)
    {
        if (maybeLayer is LayerModel layer)
        {
            Model = layer;
            Background = _thumbnailSaver.Get(layer.Root.Key);
            baseFilter = layer.Root.Inner.Metadata.ExtractFilter();
            return true;
        }

        return false;
    }

    public void UpdateSource(string label, string query, ResourceKind kind)
    {
        Results = new IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>(
            new IncrementalFactorySource<ExhibitModel>(async (page, limit, token) =>
                (await _repositoryAgent.SearchAsync(label, query, page, limit, baseFilter with { Kind = kind }, token))
                .Select(ToModel)), 10);
    }

    private void OpenResourceModal(ExhibitModel? exhibit)
    {
        if (exhibit != null)
        {
            Attachment? installed =
                Model?.Attachments.FirstOrDefault(
                    x => x.Label == exhibit.Inner.Label && x.ProjectId == exhibit.Inner.Id);
            ProjectPreviewModal modal = new ProjectPreviewModal(exhibit, _repositoryAgent, installed,
                InstallAttachmentCommand, UninstallAttachmentCommand);
            _modalService.Pop(modal);
        }
    }

    private ExhibitModel ToModel(Exhibit exhibit)
    {
        var added = Model?.Attachments.Any(x => x.Label == exhibit.Label && x.ProjectId == exhibit.Id) ?? false;
        var result = new ExhibitModel(exhibit, OpenResourceModalCommand);
        result.HasAdded.Value = added;
        return result;
    }

    private void InstallAttachment(ModpackModel? project) { }

    private void UninstallAttachment(ModpackModel? project) { }
}