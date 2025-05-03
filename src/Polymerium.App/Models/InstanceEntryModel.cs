using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceEntryModel : ModelBase
{
    public InstanceEntryModel(string key, string name, string version, string? loader, string? source) =>
        Basic = new InstanceBasicModel(key, name, version, loader, source);

    public InstanceBasicModel Basic { get; }

    #region Reactive

    [ObservableProperty]
    public partial InstanceEntryState State { get; set; }

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial bool IsPending { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset LastPlayed { get; set; } = DateTimeOffset.MinValue;

    #endregion
}