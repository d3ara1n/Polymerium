using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Models;

public partial class LiveLogSourceModel : LogSourceModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsOnAir { get; set; }

    #endregion
}
