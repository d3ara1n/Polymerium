using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class GalleryBitmapModel(Uri uri, string? title) : ModelBase
{
    public Uri Uri => uri;
    public string? Title => title;

    [ObservableProperty]
    public partial Bitmap? ThumbnailBitmap { get; set; }
}
