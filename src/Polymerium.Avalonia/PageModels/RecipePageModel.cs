using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Pref;

namespace Polymerium.Avalonia.PageModels;

public partial class RecipePageModel(
    IViewContext<string> context,
    PersistenceService persistenceService,
    DataService dataService,
    NavigationService navigationService,
    NotificationService notificationService) : ViewModelBase
{
    public string Id { get; } = context.Parameter!;

    public ObservableCollection<RecipeItemModel> Items { get; } = [];

    #region Reactive

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial string? NewItemPref { get; set; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        var recipe = persistenceService.GetRecipe(Id);
        if (recipe is not null)
        {
            Name = recipe.Name;
            Description = recipe.Description;
        }

        await ReloadItemsAsync(token);
    }

    #endregion

    private async Task ReloadItemsAsync(CancellationToken token)
    {
        var stored = persistenceService.GetRecipeItems(Id);
        var byKey = Items.ToDictionary(x => (x.Label, x.Namespace, x.ProjectId));
        Items.Clear();
        foreach (var item in stored)
        {
            var key = (item.Label, item.Namespace, item.ProjectId);
            Items.Add(byKey.TryGetValue(key, out var reused)
                ? reused
                : new(item.Id, item.Label, item.Namespace, item.ProjectId) { Note = item.Note });
        }

        await ResolveItemInfoAsync(token);
    }

    private async Task ResolveItemInfoAsync(CancellationToken token)
    {
        var pending = Items.Where(x => x.Info is null).ToList();
        if (pending.Count == 0)
        {
            return;
        }

        try
        {
            var identifiers = pending
                             .Select(x => new ProjectIdentifier(x.Label,
                                                                PersistenceService.NormalizeFavoriteNamespace(x.Namespace),
                                                                x.ProjectId))
                             .Distinct()
                             .ToList();

            var result = await Task.Run(() => dataService.QueryProjectsAsync(identifiers), token);
            var resolved = result.Successful;
            foreach (var item in pending)
            {
                var key = new ProjectIdentifier(item.Label,
                                                PersistenceService.NormalizeFavoriteNamespace(item.Namespace),
                                                item.ProjectId);
                if (resolved.TryGetValue(key, out var project))
                {
                    item.Info = project;
                }
            }
        }
        catch
        {
            // NOTE: Info 是渐进增强，解析失败时保留原始标识即可，不阻断页面。
        }
    }

    #region Commands

    [RelayCommand]
    private void SaveMetadata() => persistenceService.UpdateRecipe(Id, Name, Description);

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (string.IsNullOrWhiteSpace(NewItemPref))
        {
            return;
        }

        if (!PackageHelper.TryParse(NewItemPref, out var id))
        {
            notificationService.PopMessage(Resources.RecipePage_InvalidPrefWarningNotificationMessage,
                                           Resources.RecipePage_InvalidPrefWarningNotificationTitle,
                                           GrowlLevel.Warning);
            return;
        }

        persistenceService.AddRecipeItem(Id, id.Repository, id.Namespace, id.Identity, [], null);
        NewItemPref = null;
        await ReloadItemsAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void RemoveItem(RecipeItemModel? item)
    {
        if (item is not null)
        {
            persistenceService.RemoveRecipeItem(item.Id);
            Items.Remove(item);
        }
    }

    [RelayCommand]
    private void Export() =>
        notificationService.PopMessage(Resources.RecipesPage_ComingSoonNotificationMessage,
                                       Resources.RecipePage_ExportButtonText,
                                       GrowlLevel.Information);

    [RelayCommand]
    private void GoBack() => navigationService.GoBack();

    #endregion
}
