using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class GalleryImageModel(Uri uri, string? title) : ModelBase
{
    public Uri Uri => uri;
    public string? Title => title;

    [ObservableProperty]
    public partial Uri? Thumbnail { get; set; }
}
