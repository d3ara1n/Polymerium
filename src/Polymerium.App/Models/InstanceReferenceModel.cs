using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceReferenceModel : ModelBase
{
    #region Reactive Properties

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<InstanceReferenceVersionModel>? Versions { get; set; }

    [ObservableProperty]
    public partial InstanceReferenceVersionModel? CurrentVersion { get; set; }

    [ObservableProperty]
    public partial string? SourceLabel { get; set; }

    [ObservableProperty]
    public partial Uri? SourceUrl { get; set; }

    #endregion
}