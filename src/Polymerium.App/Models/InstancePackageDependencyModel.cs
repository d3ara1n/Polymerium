using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class InstancePackageDependencyModel(
    string label,
    string? @namespace,
    string projectId,
    string? versionId,
    string projectName,
    Bitmap thumbnail,
    Uri reference,
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
    public string ProjectId => projectId;
    public string? VersionId => versionId;
    public string ProjectName => projectName;
    public Bitmap Thumbnail => thumbnail;
    public Uri Reference => reference;
    public uint RefCount => refCount;
    public bool IsRequired => isRequired;

    #endregion
}
