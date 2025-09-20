using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public partial class InstancePackageInfoModel(
    InstancePackageModel owner,
    string label,
    string? @namespace,
    string projectId,
    string projectName,
    InstancePackageVersionModelBase version,
    string author,
    string summary,
    Uri? reference,
    Bitmap thumbnail,
    ResourceKind kind) : ModelBase
{
    #region Direct

    public InstancePackageModel Owner => owner;
    public Uri? Reference => reference;
    public string Label => label;
    public string? Namespace => @namespace;
    public string ProjectId => projectId;
    public string ProjectName => projectName;
    public string Author => author;
    public string Summary => summary;
    public Bitmap Thumbnail => thumbnail;
    public ResourceKind Kind => kind;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial InstancePackageVersionModelBase Version { get; set; } = version;

    partial void OnVersionChanged(InstancePackageVersionModelBase value)
    {
        // 这里 = new InstancePackageInfoModel 不会触发 OnVersionChanged
        owner.Entry.Purl = PackageHelper.ToPurl(label,
                                                @namespace,
                                                projectId,
                                                value is InstancePackageVersionModel v ? v.Id : null);
        owner.OldPurlCache = owner.Entry.Purl;
    }

    #endregion
}
