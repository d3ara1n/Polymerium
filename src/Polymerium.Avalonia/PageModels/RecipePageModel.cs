using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Services;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
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

    public IReadOnlyList<ResourceKind?> KindFilterOptions { get; } =
        [null, .. Enum.GetValues<ResourceKind>().Where(k => k != ResourceKind.Unknown)];

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        var queryFilter = this.WhenValueChanged(x => x.QueryText).Select(BuildQueryFilter);
        var kindFilter = this.WhenValueChanged(x => x.SelectedKind).Select(BuildKindFilter);
        _items.CountChanged.Subscribe(c => TotalCount = c).DisposeWith(_subscriptions);
        _items
            .Connect()
            .Filter(queryFilter)
            .Filter(kindFilter)
            .Bind(out var view)
            .Subscribe()
            .DisposeWith(_subscriptions);
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

    private static Func<RecipeItemModel, bool> BuildQueryFilter(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _ => true;
        }

        var q = query.Trim();
        return item => (item.Info?.ProjectName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Info?.Author?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                    || item.Label.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || item.ProjectId.Contains(q, StringComparison.OrdinalIgnoreCase);
    }

    private static Func<RecipeItemModel, bool> BuildKindFilter(ResourceKind? kind) =>
        kind is null ? _ => true : item => item.Info?.Kind == kind;

    private async Task ReloadItemsAsync(CancellationToken token)
    {
        var stored = persistenceService.GetRecipeItems(Id);
        var storedKeys = stored
                         .Select(s => new ProjectIdentifier(s.Label, PersistenceService.NormalizeFavoriteNamespace(s.Namespace), s.ProjectId))
                         .ToHashSet();

        // toRemove: cache 有、DB 无
        _items.Remove([.. _items.Keys.Where(k => !storedKeys.Contains(k))]);

        // toAdd / toUpdate: Lookup 命中则刷新 Note/Tags，未命中则新建
        var toAdd = new List<RecipeItemModel>();
        foreach (var s in stored)
        {
            var ns = PersistenceService.NormalizeFavoriteNamespace(s.Namespace);
            var id = new ProjectIdentifier(s.Label, ns, s.ProjectId);
            if (_items.Lookup(id) is { HasValue: true, Value: var existing })
            {
                existing.Note = s.Note;
                existing.Tags = DeserializeTags(s.Tags);
            }
            else
            {
                toAdd.Add(new(s.Id, s.Label, ns, s.ProjectId)
                {
                    Note = s.Note,
                    Tags = DeserializeTags(s.Tags)
                });
            }
        }

        _items.AddOrUpdate(toAdd);

        if (toAdd.Count > 0)
        {
            await ResolveItemInfoAsync(toAdd, token);
        }
    }

    private static ObservableCollection<string> DeserializeTags(string json)
    {
        try
        {
            var tags = JsonSerializer.Deserialize<string[]>(json) ?? [];
            return new ObservableCollection<string>(tags);
        }
        catch
        {
            return [];
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

    [ObservableProperty]
    public partial string QueryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ResourceKind? SelectedKind { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyText))]
    public partial int TotalCount { get; private set; }

    public string EmptyText => TotalCount == 0
        ? Resources.RecipePage_NoPackagesText
        : Resources.RecipePage_NoMatchesText;

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenItem(RecipeItemModel? item)
    {
        if (item is not null)
        {
            overlayService.PopModal<RecipeItemModal>(item);
        }
    }

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
    private void AddItem() => navigationService.Navigate<ExplorerPage>(new RecipeExplorerSession(Id,
                                                                                                persistenceService,
                                                                                                dataService,
                                                                                                overlayService));

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
    private async Task EditItemTagsAsync(RecipeItemModel? item)
    {
        if (item is null)
        {
            return;
        }

        var dialog = new TagsEditorDialog
        {
            InitialTags = item.Tags.ToArray(),
            Suggestions = item.Info?.Tags
        };
        if (await overlayService.PopDialogAsync(dialog) && dialog.Result is IReadOnlyList<string> result)
        {
            item.Tags.Clear();
            foreach (var tag in result)
            {
                item.Tags.Add(tag);
            }
            persistenceService.UpdateRecipeItem(item.Id, result, item.Note);
        }
    }

    [RelayCommand]
    private async Task EditItemNoteAsync(RecipeItemModel? item)
    {
        if (item is null)
        {
            return;
        }

        var input = await overlayService.RequestInputAsync(title: Resources.RecipePage_EditNoteMenuText,
                                                           placeholder: item.Note,
                                                           multiLine: true);
        if (input is null)
        {
            return;
        }

        item.Note = string.IsNullOrWhiteSpace(input) ? null : input;
        persistenceService.UpdateRecipeItem(item.Id, item.Tags, item.Note);
    }

    [RelayCommand]
    private void Export() =>
        notificationService.PopMessage(Resources.RecipesPage_ComingSoonNotificationMessage,
                                       Resources.RecipePage_ExportButtonText);

    [RelayCommand]
    private void GoBack() => navigationService.GoBack();

    #endregion
}
