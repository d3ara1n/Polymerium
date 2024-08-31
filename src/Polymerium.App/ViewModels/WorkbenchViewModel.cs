using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml.Media.Imaging;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using System;
using System.Collections.Generic;
using System.IO;
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
    private BitmapImage? background;
    private Filter baseFilter = Filter.EMPTY;
    private LayerModel? model;

    private IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? results;

    public WorkbenchViewModel(ThumbnailSaver thumbnailSaver, RepositoryAgent repositoryAgent,
        ModalService modalService)
    {
        _thumbnailSaver = thumbnailSaver;
        _modalService = modalService;
        _repositoryAgent = repositoryAgent;
        Repositories = repositoryAgent.Repositories.Select(x => new RepositoryModel(x.Label, x.Label switch
        {
            RepositoryLabels.CURSEFORGE => AssetPath.HEADER_CURSEFORGE,
            RepositoryLabels.MODRINTH => AssetPath.HEADER_MODRINTH,
            _ => throw new NotImplementedException()
        }));
        OpenResourceModalCommand = new RelayCommand<ExhibitModel>(OpenResourceModal);
    }

    public LayerModel? Model
    {
        get => model;
        set => SetProperty(ref model, value);
    }

    public BitmapImage? Background
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
    public IEnumerable<RepositoryModel> Repositories { get; }

    public override bool OnAttached(object? maybeLayer)
    {
        if (maybeLayer is LayerModel layer)
        {
            Model = layer;
            var path = _thumbnailSaver.Get(layer.Root.Key);
            if (path != null && File.Exists(path))
            {
                Background = new BitmapImage(new Uri(path));
            }

            baseFilter = layer.Root.Inner.Metadata.ExtractFilter();
            return true;
        }

        return false;
    }

    public void UpdateSource(string label, string query, ResourceKind kind) =>
        Results = new IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>(
            new IncrementalFactorySource<ExhibitModel>(async (page, limit, token) =>
                (await _repositoryAgent.SearchAsync(label, query, page, limit, baseFilter with { Kind = kind },
                    token))
                .Select(ToModel)), 10);

    private void OpenResourceModal(ExhibitModel? exhibit)
    {
        if (Model != null && exhibit != null)
        {
            ProjectPreviewModal modal = new(_repositoryAgent, exhibit.Inner.Label, exhibit.Inner.Id,
                Model.Root.Inner.Metadata.ExtractFilter() ?? Filter.EMPTY, Model, _ => exhibit.HasAdded.Value = true);
            _modalService.Pop(modal);
        }
    }

    private ExhibitModel ToModel(Exhibit exhibit)
    {
        var added = Model?.Root.Inner.Metadata.Layers.SelectMany(x => x.Attachments)
            .Any(x => x.Label == exhibit.Label && x.ProjectId == exhibit.Id) ?? false;
        ExhibitModel result = new(exhibit, OpenResourceModalCommand);
        result.HasAdded.Value = added;
        return result;
    }
}