using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstancePackageDependencyModel(
    string label,
    string? @namespace,
    string pid,
    string? vid,
    string projectName,
    Bitmap thumbnail,
    uint refCount,
    bool isRequired) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial InstancePackageModel? Installed { get; set; }

    #endregion

    #region Direct

    public string Label => label;
    public string? Namespace => @namespace;
    public string Pid => pid;
    public string? Vid => vid;
    public string ProjectName => projectName;
    public Bitmap Thumbnail => thumbnail;
    public uint RefCount => refCount;
    public bool IsRequired => isRequired;

    #endregion
}
