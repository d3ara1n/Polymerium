using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Models;

public partial class LiveLogSourceModel : LogSourceModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsOnAir { get; set; }

    #endregion
}
