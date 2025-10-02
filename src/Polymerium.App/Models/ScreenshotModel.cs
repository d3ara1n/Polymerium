using System;
using System.IO;
using Avalonia.Media.Imaging;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class ScreenshotModel : ModelBase
{
    public ScreenshotModel(Uri image, DateTimeOffset time)
    {
        Image = image;
        TimeRaw = time;
        using var stream = File.OpenRead(image.LocalPath);
        var memory = new MemoryStream();
        stream.CopyTo(memory);
        memory.Position = 0;
        Thumbnail = Bitmap.DecodeToWidth(memory, 256, BitmapInterpolationMode.LowQuality);
    }

    #region Direct

    public DateTimeOffset TimeRaw { get; }
    public string Time => TimeRaw.ToString("t");

    public Uri Image { get; }
    public Bitmap Thumbnail { get; }

    #endregion
}
