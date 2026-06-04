using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.App.Models;

public enum ConnectionTestStatus
{
    PENDING,
    TESTING,
    SUCCESS,
    FAILED,
}

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
    public partial ConnectionTestStatus Status { get; set; } = ConnectionTestStatus.PENDING;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    #endregion
}
