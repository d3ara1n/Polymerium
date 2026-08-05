using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using System.IO;
using System.Linq;
using Polymerium.Avalonia.Utilities;
using TridentCore.Pref;

namespace Polymerium.Avalonia.PageModels;

public partial class RecipesPageModel(
    PersistenceService persistenceService,
    NavigationService navigationService,
    OverlayService overlayService,
    InstanceService instanceService,
    NotificationService notificationService) : ViewModelBase
{
    public ObservableCollection<RecipeCardModel> Items { get; } = [];

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        foreach (var recipe in persistenceService.GetRecipes())
        {
            Items.Add(new(recipe.Id)
            {
                Name = recipe.Name,
                Description = recipe.Description,
                ItemCount = persistenceService.CountRecipeItems(recipe.Id)
            });
        }

        return base.OnInitializeAsync(token);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Open(RecipeCardModel? card)
    {
        if (card is not null)
        {
            navigationService.Navigate<RecipePage>(card.Id);
        }
    }

    [RelayCommand]
    private async Task EditAsync(RecipeCardModel? card)
    {
        if (card is null)
        {
            return;
        }

        var dialog = new RecipeEditorDialog
        {
            Title = Resources.RecipeEditorDialog_EditTitle,
            RecipeName = card.Name,
            RecipeDescription = card.Description
        };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is RecipeEditorResultModel result)
        {
            persistenceService.UpdateRecipe(card.Id, result.Name, result.Description);
            card.Name = result.Name;
            card.Description = result.Description;
        }
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        var dialog = new RecipeEditorDialog { Title = Resources.RecipesPage_NewDialogTitle };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is RecipeEditorResultModel result)
        {
            var name = string.IsNullOrWhiteSpace(result.Name) ? "Untitled" : result.Name;
            var recipe = persistenceService.InsertRecipe(name, result.Description);
            Items.Add(new(recipe.Id)
            {
                Name = recipe.Name,
                Description = recipe.Description,
                ItemCount = 0
            });
        }
    }

    [RelayCommand]
    private async Task ExportAsync(RecipeCardModel? card)
    {
        if (card is null)
        {
            return;
        }

        var recipe = persistenceService.GetRecipe(card.Id);
        if (recipe is null)
        {
            return;
        }

        var items = persistenceService.GetRecipeItems(card.Id);
        var document = RecipeHelper.ToDocument(recipe.Name,
                                                recipe.Description,
                                                items.Select(i => (i.Label,
                                                                    PersistenceService.NormalizeNamespace(i.Namespace),
                                                                    i.ProjectId,
                                                                    RecipeHelper.DeserializeTags(i.Tags),
                                                                    i.Note)));
        var dialog = new RecipeExporterDialog { ItemCount = items.Count, RecipeName = recipe.Name };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is string path)
        {
            try
            {
                await Task.Run(() => File.WriteAllText(path, RecipeHelper.Serialize(document)));
                notificationService.PopMessage(Resources.RecipesPage_ExportSuccessNotificationMessage.Replace("{0}", path),
                                               Resources.RecipesPage_ExportSuccessNotificationTitle,
                                               GrowlLevel.Success);
            }
            catch (Exception)
            {
                notificationService.PopMessage(Resources.RecipesPage_ExportDangerNotificationMessage.Replace("{0}", path),
                                               Resources.RecipesPage_ExportDangerNotificationTitle,
                                               GrowlLevel.Danger);
            }
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var path = await overlayService.RequestFileAsync();
        if (path is null)
        {
            return;
        }

        string text;
        try
        {
            text = await File.ReadAllTextAsync(path);
        }
        catch (Exception)
        {
            notificationService.PopMessage(Resources.RecipesPage_ImportDangerNotificationMessage,
                                           Resources.RecipesPage_ImportDangerNotificationTitle,
                                           GrowlLevel.Danger);
            return;
        }

        if (!RecipeHelper.TryDeserialize(text, out var document))
        {
            notificationService.PopMessage(Resources.RecipesPage_ImportDangerNotificationMessage,
                                           Resources.RecipesPage_ImportDangerNotificationTitle,
                                           GrowlLevel.Danger);
            return;
        }

        var name = string.IsNullOrWhiteSpace(document.Name) ? "Untitled" : document.Name;
        var recipe = persistenceService.InsertRecipe(name, document.Description);
        var added = 0;
        var seen = new HashSet<ProjectIdentifier>();
        foreach (var item in document.Items)
        {
            if (!RecipeHelper.TryExtractIdentity(item, out var label, out var ns, out var projectId))
            {
                continue;
            }

            var identifier = new ProjectIdentifier(label, ns, projectId);
            if (!seen.Add(identifier))
            {
                continue;
            }

            persistenceService.AddRecipeItem(recipe.Id, identifier, item.Tags, item.Note);
            added++;
        }

        Items.Add(new(recipe.Id)
        {
            Name = recipe.Name,
            Description = recipe.Description,
            ItemCount = added
        });
        notificationService.PopMessage(Resources.RecipesPage_ImportSuccessNotificationMessage,
                                       recipe.Name,
                                       GrowlLevel.Success);
    }

    [RelayCommand]
    private async Task DeleteAsync(RecipeCardModel? card)
    {
        if (card is null)
        {
            return;
        }

        var references = instanceService.GetRecipeReferences(card.Id);
        if (references.Count > 0)
        {
            notificationService.PopMessage(Resources.RecipesPage_DeleteBlockedByReferencesWarningNotificationMessage
                                                    .Replace("{0}", references.Count.ToString()),
                                           Resources.RecipesPage_DeleteBlockedByReferencesWarningNotificationTitle,
                                           GrowlLevel.Warning);
            return;
        }

        if (!await overlayService.RequestStrongConfirmationAsync(Resources.RecipesPage_DeleteConfirmationMessage,
                                                                 Resources.RecipesPage_DeleteConfirmationTitle))
        {
            return;
        }

        persistenceService.DeleteRecipe(card.Id);
        Items.Remove(card);
        notificationService.PopMessage(Resources.RecipesPage_DeleteSuccessNotificationMessage,
                                       card.Name,
                                       GrowlLevel.Success);
    }

    #endregion
}
