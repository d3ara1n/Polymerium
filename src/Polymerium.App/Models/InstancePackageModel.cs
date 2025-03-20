using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class InstancePackageModel(string purl) : ModelBase
{
    // 移动所有权到内部
    internal Task? Task;

    #region Direct Properties

    public string Purl => purl;

    #endregion

    #region Rective Properties

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? Version { get; set; }

    [ObservableProperty]
    public partial string? Summary { get; set; }

    [ObservableProperty]
    public partial Uri? Reference { get; set; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    [ObservableProperty]
    public partial ResourceKind? Kind { get; set; }

    [ObservableProperty]
    public partial Control? Container { get; set; }

    #endregion
}