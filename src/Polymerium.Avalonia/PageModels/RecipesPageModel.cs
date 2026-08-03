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
                Description = recipe.Description
            });
            navigationService.Navigate<RecipePage>(recipe.Id);
        }
    }

    [RelayCommand]
    private void Export(RecipeCardModel? card) =>
        notificationService.PopMessage(Resources.RecipesPage_ComingSoonNotificationMessage,
                                       Resources.RecipesPage_ExportMenuText);

    [RelayCommand]
    private void Import() =>
        notificationService.PopMessage(Resources.RecipesPage_ComingSoonNotificationMessage,
                                       Resources.RecipesPage_ImportButtonText);

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
