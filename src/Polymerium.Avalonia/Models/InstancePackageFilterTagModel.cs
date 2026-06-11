using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public partial class InstancePackageFilterTagModel(string content) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    #endregion

    #region Direct

    internal int RefCount { get; set; }

    public string Content => content;

    #endregion
}
