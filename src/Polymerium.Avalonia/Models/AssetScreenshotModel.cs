using System;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class AssetScreenshotModel : ModelBase
{
    public AssetScreenshotModel(Uri image, Uri thumbnail, DateTimeOffset time, bool isLocked)
    {
        Image = image;
        TimeRaw = time;
        IsLocked = isLocked;
        Thumbnail = thumbnail;
    }

    #region Direct

    public DateTimeOffset TimeRaw { get; }
    public bool IsLocked { get; }
    public string Time => TimeRaw.ToString("t");

    public Uri Image { get; }
    public Uri Thumbnail { get; }

    #endregion
}
