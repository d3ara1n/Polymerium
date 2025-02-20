using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceEntryModel : ModelBase
{
    public InstanceEntryModel(string key, string name, string version, string? loader, string? source) =>
        Basic = new InstanceBasicModel(key, name, version, loader, source);

    public InstanceBasicModel Basic { get; }

    #region Reactive Properties

    [ObservableProperty] private InstanceEntryState _state;
    [ObservableProperty] private double? _progress;

    #endregion
}