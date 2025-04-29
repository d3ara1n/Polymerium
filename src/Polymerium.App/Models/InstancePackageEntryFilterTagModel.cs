using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstancePackageEntryFilterTagModel(string content) : ModelBase
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