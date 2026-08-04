using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Utilities;
using TridentCore.Pref;

namespace Polymerium.Avalonia.PageModels;

public partial class RecipePageModel(
    IViewContext<string> context,
    PersistenceService persistenceService,
    DataService dataService,
    NavigationService navigationService,
    NotificationService notificationService,
    OverlayService overlayService) : ViewModelBase
{
    public string Id { get; } = context.Parameter!;

    private readonly SourceCache<RecipeItemModel, ProjectIdentifier> _items = new(x => x.Identifier);

    private readonly CompositeDisposable _subscriptions = new();

    [ObservableProperty]
    public partial ReadOnlyObservableCollection<RecipeItemModel>? Items { get; set; }

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        _items.Connect().Bind(out var view).Subscribe().DisposeWith(_subscriptions);
        Items = view;

        var recipe = persistenceService.GetRecipe(Id);
        if (recipe is not null)
        {
            Name = recipe.Name;
            Description = recipe.Description;
        }

        await ReloadItemsAsync(token);
    }

    protected override Task OnDeinitializeAsync()
    {
        _subscriptions.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    private async Task ReloadItemsAsync(CancellationToken token)
    {
        var stored = persistenceService.GetRecipeItems(Id);
        var storedKeys = stored
                         .Select(s => new ProjectIdentifier(s.Label, PersistenceService.NormalizeFavoriteNamespace(s.Namespace), s.ProjectId))
                         .ToHashSet();

        // toRemove: cache 有、DB 无
        _items.Remove([.. _items.Keys.Where(k => !storedKeys.Contains(k))]);

        // toAdd / toUpdate: Lookup 命中则刷新 Note，未命中则新建
        var toAdd = new List<RecipeItemModel>();
        foreach (var s in stored)
        {
            var ns = PersistenceService.NormalizeFavoriteNamespace(s.Namespace);
            var id = new ProjectIdentifier(s.Label, ns, s.ProjectId);
            if (_items.Lookup(id) is { HasValue: true, Value: var existing })
            {
                existing.Note = s.Note;
            }
            else
            {
                toAdd.Add(new(s.Id, s.Label, ns, s.ProjectId) { Note = s.Note });
            }
        }

        _items.AddOrUpdate(toAdd);

        if (toAdd.Count > 0)
        {
            await ResolveItemInfoAsync(toAdd, token);
        }
    }

    private async Task ResolveItemInfoAsync(IReadOnlyList<RecipeItemModel> pending, CancellationToken token)
    {
        IsRefreshing = true;
        try
        {
            var identifiers = pending.Select(x => x.Identifier).Distinct().ToList();

            var result = await Task.Run(() => dataService.QueryProjectsAsync(identifiers), token);
            var resolved = result.Successful;
            foreach (var item in pending)
            {
                if (resolved.TryGetValue(item.Identifier, out var project))
                {
                    item.Info = project;
                }

                item.IsLoaded = true;
            }
        }
        catch
        {
            // NOTE: 渐进增强：解析失败时保留原始标识并标记已加载以降级显示，不阻断页面
            foreach (var item in pending)
            {
                item.IsLoaded = true;
            }
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    #region Reactive

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial bool IsRefreshing { get; set; }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task EditAsync()
    {
        var dialog = new RecipeEditorDialog
        {
            Title = Resources.RecipeEditorDialog_EditTitle,
            RecipeName = Name,
            RecipeDescription = Description
        };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is RecipeEditorResultModel result)
        {
            Name = result.Name;
            Description = result.Description;
            persistenceService.UpdateRecipe(Id, Name, Description);
        }
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        var input = await overlayService.RequestInputAsync(message: Resources.RecipePage_AddPackagePromptMessage);
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        if (!PackageHelper.TryParse(input, out var id))
        {
            notificationService.PopMessage(Resources.RecipePage_AddPackageInvalidPrefWarningMessage,
                                           Resources.RecipePage_AddPackageInvalidPrefWarningTitle,
                                           GrowlLevel.Warning);
            return;
        }

        persistenceService.AddRecipeItem(Id, id.Repository, id.Namespace, id.Identity, [], null);
        await ReloadItemsAsync(CancellationToken.None);
    }

    [RelayCommand]
    private void RemoveItem(RecipeItemModel? item)
    {
        if (item is not null)
        {
            persistenceService.RemoveRecipeItem(item.Id);
            _items.Remove(item.Identifier);
        }
    }

    [RelayCommand]
    private void Export() =>
        notificationService.PopMessage(Resources.RecipesPage_ComingSoonNotificationMessage,
                                       Resources.RecipePage_ExportButtonText);

    [RelayCommand]
    private void GoBack() => navigationService.GoBack();

    #endregion
}
