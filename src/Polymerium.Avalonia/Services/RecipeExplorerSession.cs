using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Pref;

namespace Polymerium.Avalonia.Services;

public sealed class RecipeExplorerSession : ExplorerSession
{
    private readonly string _recipeId;
    private readonly PersistenceService _persistenceService;
    private readonly DataService _dataService;
    private readonly OverlayService _overlayService;
    private string _title = string.Empty;
    private Dictionary<ProjectIdentifier, string> _knownItems = [];

    public override Bitmap? Background => null;

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

    public override void Validate()
    {
        _title = _persistenceService.GetRecipe(_recipeId)?.Name ?? string.Empty;
        _knownItems = LoadItems();
        if (string.IsNullOrEmpty(_title))
        {
            throw new PageNotReachedException(typeof(ExplorerPage), "The recipe is not found");
        }
    }

    public override string Title => _title;

    public override Filter? InitialFilter => null;

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

    public override async Task ViewExhibitAsync(ExhibitModel exhibit,
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
                                                      model.Label,
                                                      model.Namespace,
                                                      model.ProjectId,
                                                      [],
                                                      null);
                    model.State = ExhibitState.Editable;
                    break;
                }
                case { State: ExhibitState.Removing }:
                {
                    var identifier = new ProjectIdentifier(model.Label,
                                                           PersistenceService.NormalizeFavoriteNamespace(model.Namespace),
                                                           model.ProjectId);
                    if (_knownItems.Remove(identifier, out var itemId))
                    {
                        _persistenceService.RemoveRecipeItem(itemId);
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
        _knownItems.ContainsKey(new ProjectIdentifier(label,
                                                      PersistenceService.NormalizeFavoriteNamespace(@namespace),
                                                      projectId));

    private Dictionary<ProjectIdentifier, string> LoadItems() =>
        _persistenceService.GetRecipeItems(_recipeId)
                           .ToDictionary(x => new ProjectIdentifier(x.Label,
                                                                    PersistenceService.NormalizeFavoriteNamespace(x.Namespace),
                                                                    x.ProjectId),
                                         x => x.Id);
}
