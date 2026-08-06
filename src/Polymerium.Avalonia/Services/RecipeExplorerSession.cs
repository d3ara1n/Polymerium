using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Pref;

namespace Polymerium.Avalonia.Services;

public sealed class RecipeExplorerSession : ExplorerSession
{
    private readonly DataService _dataService;
    private readonly OverlayService _overlayService;
    private readonly PersistenceService _persistenceService;
    private readonly string _recipeId;
    private HashSet<ProjectIdentifier> _knownItems = [];
    private string _title = string.Empty;

    public RecipeExplorerSession(
        string recipeId,
        PersistenceService persistenceService,
        DataService dataService,
        OverlayService overlayService)
    {
        _recipeId = recipeId;
        _persistenceService = persistenceService;
        _dataService = dataService;
        _overlayService = overlayService;
    }

    public override Bitmap? Background => null;

    public override string Title => _title;

    public override Filter? InitialFilter => null;

    public override void Validate()
    {
        _title = _persistenceService.GetRecipe(_recipeId)?.Name ?? string.Empty;
        _knownItems = LoadItems();
        if (string.IsNullOrEmpty(_title))
        {
            throw new PageNotReachedException(typeof(ExplorerPage), "The recipe is not found");
        }
    }

    public override ExhibitModel BuildExhibit(Exhibit hit) =>
        new(hit.Label,
            hit.Namespace,
            hit.Pid,
            hit.Name,
            hit.Summary,
            hit.Thumbnail ?? AssetUriIndex.DirtImage,
            hit.Author,
            hit.Tags,
            hit.UpdatedAt,
            hit.DownloadCount,
            hit.Reference)
        {
            State = IsKnown(hit.Label, hit.Namespace, hit.Pid) ? ExhibitState.Editable : null,
            IsFavorite = _persistenceService.IsFavoriteProject(hit.Label, hit.Namespace, hit.Pid)
        };

    public override void RevertState(ExhibitModel exhibit) =>
        exhibit.State = IsKnown(exhibit.Label, exhibit.Namespace, exhibit.ProjectId) ? ExhibitState.Editable : null;

    public override async Task ViewExhibitAsync(
        ExhibitModel exhibit,
        Action<ExhibitModel> modifyPending,
        Func<ProjectIdentifier, ExhibitModel?> findExisting)
    {
        var project = await _dataService.QueryProjectAsync(new(exhibit.Label, exhibit.Namespace, exhibit.ProjectId));

        var model = new ExhibitPackageModel(exhibit.Label,
                                            exhibit.Namespace,
                                            exhibit.ProjectId,
                                            project.ProjectName,
                                            project.Author,
                                            project.Reference,
                                            project.Thumbnail,
                                            project.Tags,
                                            project.DownloadCount,
                                            project.Summary,
                                            project.UpdatedAt,
                                            [.. project.Gallery.Select(x => x.Url)]);

        _overlayService.PopModal(new ExhibitProjectModal
        {
            PersistenceService = _persistenceService,
            DataContext = model,
            Exhibit = exhibit,
            DataService = _dataService,
            Kind = project.Kind,
            ModifyPendingCallback = modifyPending,
            UndoCallback = m =>
            {
                RevertState(m);
                modifyPending(m);
            }
        });
    }

    public override Task<bool> CollectAsync(IReadOnlyList<ExhibitModel> pending)
    {
        foreach (var model in pending)
        {
            switch (model)
            {
                case { State: ExhibitState.Adding }:
                    {
                        _persistenceService.AddRecipeItem(_recipeId,
                                                          new(model.Label,
                                                              PersistenceService.NormalizeNamespace(model.Namespace),
                                                              model.ProjectId),
                                                          [],
                                                          null);
                        model.State = ExhibitState.Editable;
                        break;
                    }
                case { State: ExhibitState.Removing }:
                    {
                        var identifier = new ProjectIdentifier(model.Label,
                                                               PersistenceService.NormalizeNamespace(model.Namespace),
                                                               model.ProjectId);
                        if (_knownItems.Remove(identifier))
                        {
                            _persistenceService.RemoveRecipeItem(_recipeId, identifier);
                        }

                        model.State = null;
                        break;
                    }
            }
        }

        _knownItems = LoadItems();
        return Task.FromResult(true);
    }

    private bool IsKnown(string label, string? @namespace, string projectId) =>
        _knownItems.Contains(new(label, PersistenceService.NormalizeNamespace(@namespace), projectId));

    private HashSet<ProjectIdentifier> LoadItems() =>
        _persistenceService
           .GetRecipeItems(_recipeId)
           .Select(x => new ProjectIdentifier(x.Label, PersistenceService.NormalizeNamespace(x.Namespace), x.ProjectId))
           .ToHashSet();
}
