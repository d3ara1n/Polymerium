using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Core.Engines.Launching;

namespace Polymerium.Avalonia.Models;

public partial class ScrapModel(
    string message,
    ScrapLevel level,
    string? date,
    string? time,
    string? thread,
    string? sender
) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial string Message { get; set; } = message;

    #endregion

    #region Direct

    public ScrapLevel Level => level;
    public string? Date => date;
    public string? Time => time;
    public string? Thread => thread;
    public string? Sender => sender;

    #endregion
}
