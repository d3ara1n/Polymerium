using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Models;

public enum ConnectionTestStatus { Pending, Testing, Success, Failed }

public partial class ConnectionTestSiteModel(string display, Uri endpoint) : ObservableObject
{
    #region Direct

    public string Display => display;
    public Uri Endpoint => endpoint;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial double Latency { get; set; }

    [ObservableProperty]
    public partial bool IsTesting { get; set; }

    [ObservableProperty]
    public partial ConnectionTestStatus Status { get; set; } = ConnectionTestStatus.Pending;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    #endregion
}
