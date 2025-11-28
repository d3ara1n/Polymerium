using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class FilePickerDialog : Dialog
{
    public FilePickerDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) => result is string filePath && File.Exists(filePath);

    private void DropZone_OnDragOver(object? sender, DropZone.DragOverEventArgs e)
    {
			if (e.Data.Contains(DataFormat.File))
        {
            e.Accepted = true;
        }
    }

    private void DropZone_OnDrop(object? sender, DropZone.DropEventArgs e)
    {
			if (e.Data.Contains(DataFormat.File))
        {
				var file = e.Data.TryGetFile();
            if (file != null)
            {
                e.Model = file.TryGetLocalPath();
            }
        }
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top != null)
        {
            var storage = top.StorageProvider;
            if (storage.CanOpen)
            {
                var files = await storage.OpenFilePickerAsync(new());
                var file = files.FirstOrDefault();
                if (file != null)
                {
                    Result = file.TryGetLocalPath();
                }
            }
        }
    }
}
