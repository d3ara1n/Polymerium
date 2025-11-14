using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class NotificationModel : ModelBase
{
    #region Direct

    public required string Title { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset PublishedAtRaw { get; init; }
    public required Bitmap? Thumbnail { get; init; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsDismissed { get; set; }

    #endregion
}
