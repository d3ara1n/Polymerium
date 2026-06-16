using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions;

namespace Polymerium.Avalonia.Models;

public partial class InstanceEntryModel : ModelBase
{
    public InstanceEntryModel(
        string key,
        string name,
        string version,
        string? loader,
        string? source
    ) => Basic = new(key, name, version, loader, source);

    public InstanceBasicModel Basic { get; }

    #region Reactive

    [ObservableProperty]
    public partial InstanceState State { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial bool IsPending { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LastPlayedAt))]
    public partial DateTimeOffset? LastPlayedAtRaw { get; set; }

    public string LastPlayedAt => LastPlayedAtRaw.Humanize();

    #endregion
}
