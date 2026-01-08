using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class RecentPlayModel : ModelBase
{
    #region Direct

    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string LoaderLabel { get; init; }
    public required Bitmap Thumbnail { get; init; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial AccountModel? Account { get; set; }

    [ObservableProperty]
    public required partial DateTimeOffset LastPlayedRaw { get; set; }

    public string LastPlayed => LastPlayedRaw.Humanize();

    public ObservableCollection<RecentPlayScreenshotModel> Screenshots { get; } = [];

    #endregion
}
