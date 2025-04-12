using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Models;

public partial class InstancePackageVersionModel(string id, string name) : InstancePackageVersionModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial string Id { get; set; } = id;

    [ObservableProperty]
    public partial string Name { get; set; } = name;

    #endregion
}