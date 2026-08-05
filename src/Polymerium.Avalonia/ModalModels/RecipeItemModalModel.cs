using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Dialogs;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;

namespace Polymerium.Avalonia.ModalModels;

public partial class RecipeItemModalModel(
    IViewContext<RecipeItemModel> context,
    PersistenceService persistenceService,
    OverlayService overlayService) : ViewModelBase
{
    public RecipeItemModel Item { get; } = context.Parameter!;

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        NoteDraft = Item.Note;
        TagsDraft = new ObservableCollection<string>(Item.Tags);
        return Task.CompletedTask;
    }

    [ObservableProperty]
    public partial string? NoteDraft { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> TagsDraft { get; set; } = [];

    [RelayCommand]
    private async Task EditTagsAsync()
    {
        var dialog = new TagsEditorDialog
        {
            InitialTags = TagsDraft.ToArray(),
            Suggestions = Item.Info?.Tags
        };
        if (await overlayService.PopDialogAsync(dialog)
            && dialog.Result is System.Collections.Generic.IReadOnlyList<string> result)
        {
            TagsDraft = new ObservableCollection<string>(result);
        }
    }

    [RelayCommand]
    private void Save(Modal? self)
    {
        Item.Note = NoteDraft;
        Item.Tags = new ObservableCollection<string>(TagsDraft);
        persistenceService.UpdateRecipeItem(Item.RecipeId, Item.Identifier, TagsDraft.ToArray(), NoteDraft);
        self?.Dismiss();
    }
}
