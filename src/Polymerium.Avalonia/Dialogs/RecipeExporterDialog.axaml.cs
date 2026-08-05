using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Dialogs;

public partial class RecipeExporterDialog : Dialog
{
    public static readonly DirectProperty<RecipeExporterDialog, int> ItemCountProperty =
        AvaloniaProperty.RegisterDirect<RecipeExporterDialog, int>(nameof(ItemCount),
                                                                    o => o.ItemCount,
                                                                    (o, v) => o.ItemCount = v);

    public RecipeExporterDialog() => InitializeComponent();

    public required int ItemCount
    {
        get;
        set => SetAndRaise(ItemCountProperty, ref field, value);
    }

    public required string RecipeName { get; init; }

    protected override bool ValidateResult(object? result)
    {
        if (result is string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir))
            {
                return true;
            }
        }

        return false;
    }

    #region Commands

    [RelayCommand]
    private async Task Browse()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            var storage = top.StorageProvider;
            if (storage.CanOpen)
            {
                var file = await storage.SaveFilePickerAsync(new()
                {
                    SuggestedStartLocation =
                        await storage
                           .TryGetWellKnownFolderAsync(WellKnownFolder
                                                          .Downloads),
                    SuggestedFileName = $"{RecipeName}.recipe.json",
                    DefaultExtension = "json"
                });
                if (file != null)
                {
                    Result = file.TryGetLocalPath();
                }
            }
        }
    }

    #endregion
}
