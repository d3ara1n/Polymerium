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
    [NotifyPropertyChangedFor(nameof(LastPlayed))]
    public required partial DateTimeOffset LastPlayedRaw { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastPlayTime))]
    public required partial TimeSpan LastPlayTimeRaw { get; set; }

    public string LastPlayTime => LastPlayTimeRaw.Humanize(maxUnit: TimeUnit.Day, minUnit: TimeUnit.Second);

    public string LastPlayed => LastPlayedRaw.Humanize();

    #endregion
}
