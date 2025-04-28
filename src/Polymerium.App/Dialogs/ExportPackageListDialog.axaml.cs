using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class ExportPackageListDialog : Dialog
{
    public ExportPackageListDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result)
    {
        if (result is string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (dir != null && Directory.Exists(dir))
                return true;
        }

        return false;
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            var storage = top.StorageProvider;
            if (storage.CanOpen)
            {
                var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    SuggestedStartLocation =
                        await storage
                           .TryGetWellKnownFolderAsync(WellKnownFolder
                                                          .Downloads)
                });
                var file = files.FirstOrDefault();
                if (file != null)
                    Result = file.TryGetLocalPath();
            }
        }
    }
}