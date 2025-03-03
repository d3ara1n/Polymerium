using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Controls;
using Polymerium.App.Controls;
using Polymerium.Trident.Utilities;
using System.Diagnostics;
using System.Linq;

namespace Polymerium.App.Views;

public partial class NewInstanceView : ScopedPage
{
    public NewInstanceView() => InitializeComponent();

    private void DropZone_OnDragOver(object? sender, DropZone.DragOverEventArgs e)
    {
        e.Accepted = true;
    }

    private void DropZone_OnDrop(object? sender, DropZone.DropEventArgs e)
    {
        Debug.WriteLine(string.Join(",", e.Data.GetDataFormats()));
        if (e.Data.Contains(DataFormats.Files) && e.Data.Contains("FileContents"))
        {
            var first = e.Data.GetFiles()?.FirstOrDefault();
            if (first != null)
            {
                try
                {
                    var path = first.TryGetLocalPath();
                    if (path != null && FileHelper.IsBitmapFile(path))
                    {
                        e.Model = new Bitmap(path);
                    }
                }
                catch
                {
                    // TODO: do nothing
                }
            }
        }
    }
}