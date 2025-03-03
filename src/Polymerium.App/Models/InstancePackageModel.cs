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
    private bool _isLoaded;

    [ObservableProperty]
    private string? _name;

    [ObservableProperty]
    private string? _version;

    [ObservableProperty]
    private string? _summary;

    [ObservableProperty]
    private Uri? _reference;

    [ObservableProperty]
    private Bitmap? _thumbnail;

    [ObservableProperty]
    private ResourceKind? _kind;

    [ObservableProperty]
    private Control? _container;

    #endregion
}