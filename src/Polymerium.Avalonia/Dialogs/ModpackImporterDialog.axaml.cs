using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.DialogModels;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class ModpackImporterDialog : Dialog
{
    public ModpackImporterDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) => result is ModpackImporterResult;

    private void DropZone_OnDragOver(object? sender, DropZone.DragOverEventArgs e)
    {
        if (e.Data.Contains(DataFormat.File) || e.Data.Contains(DataFormat.Text))
        {
            e.Accepted = true;
        }
    }

    private void DropZone_OnDrop(object? sender, DropZone.DropEventArgs e)
    {
        string? value = null;
        if (e.Data.Contains(DataFormat.File))
        {
            value = e.Data.TryGetFile()?.TryGetLocalPath();
        }

        value ??= e.Data.TryGetText();
        if (!string.IsNullOrEmpty(value))
        {
            e.Model = value;
        }
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider.CanOpen != true)
        {
            return;
        }

        var files = await top.StorageProvider.OpenFilePickerAsync(new());
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path != null && DataContext is ModpackImporterDialogModel model)
        {
            model.Input = path;
        }
    }
}
