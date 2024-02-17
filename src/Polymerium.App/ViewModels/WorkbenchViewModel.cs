using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
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
                (await _repositoryAgent.SearchAsync(label, query, page, limit, baseFilter with
                {
                    Kind = kind
                }, token)).Select(ToModel)), 10);
    }

    private void OpenResourceModal(ExhibitModel? exhibit)
    {
        if (exhibit != null)
        {
            var modal = new ExhibitPreviewModal(exhibit);
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
}