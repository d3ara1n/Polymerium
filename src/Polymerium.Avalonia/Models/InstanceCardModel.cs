using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class InstanceCardModel : ModelBase
{
    public InstanceCardModel(
        string key,
        string name,
        string version,
        string? loader,
        string? source
    ) => Basic = new(key, name, version, loader, source);

    public InstanceBasicModel Basic { get; }

    #region Reactive

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastPlayedAt))]
    public partial DateTimeOffset? LastPlayedAtRaw { get; set; }

    public string LastPlayedAt => LastPlayedAtRaw.Humanize();

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    public ObservableCollection<string> Tags { get; } = [];

    #endregion
}
