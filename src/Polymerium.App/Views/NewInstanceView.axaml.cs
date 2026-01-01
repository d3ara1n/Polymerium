using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Huskui.Avalonia.Controls;
using Polymerium.App.Controls;
using Trident.Core.Utilities;

namespace Polymerium.App.Views;

public partial class NewInstanceView : ScopedPage
{
    private static readonly DataFormat FileContentsFormat = DataFormat.CreateStringPlatformFormat("FileContents");

    public NewInstanceView() => InitializeComponent();

    private void DropZone_OnDragOver(object? sender, DropZone.DragOverEventArgs e)
    {
        if (e.Data.Contains(DataFormat.File) && e.Data.Contains(FileContentsFormat))
        {
            e.Accepted = true;
        }
    }

    private void DropZone_OnDrop(object? sender, DropZone.DropEventArgs e)
    {
        if (e.Data.Contains(DataFormat.File) && e.Data.Contains(FileContentsFormat))
        {
            var first = e.Data.TryGetFile();
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
                    // do nothing
                }
            }
        }
    }
}
