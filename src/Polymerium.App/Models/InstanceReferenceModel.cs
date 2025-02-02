using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstanceReferenceModel : ModelBase
{
    #region Reactive Properties

    [ObservableProperty] private string? _name;
    [ObservableProperty] private Bitmap? _thumbnail;
    [ObservableProperty] private IReadOnlyList<InstanceVersionModel>? _versions;
    [ObservableProperty] private InstanceVersionModel? _currentVersion;
    [ObservableProperty] private string? _sourceLabel;
    [ObservableProperty] private Uri? _sourceUrl;

    #endregion
}