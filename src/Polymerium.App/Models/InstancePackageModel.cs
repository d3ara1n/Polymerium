using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public partial class InstancePackageModel(
    Profile.Rice.Entry entry,
    bool isLocked,
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

    public Profile.Rice.Entry Entry => entry;
    public Uri? Reference => reference;
    public string Label => label;
    public string? Namespace => @namespace;
    public string ProjectId => projectId;
    public string ProjectName => projectName;
    public string Author => author;
    public string Summary => summary;
    public Bitmap Thumbnail => thumbnail;
    public ResourceKind Kind => kind;

    public MappingCollection<string, string> Tags { get; } = new(entry.Tags, x => x, x => x);

    #endregion

    #region Reactive

    [ObservableProperty] public partial bool IsLocked { get; set; } = isLocked;

    [ObservableProperty] public partial bool IsEnabled { get; set; } = entry.Enabled;

    partial void OnIsEnabledChanged(bool value) => entry.Enabled = value;

    [ObservableProperty] public partial InstancePackageVersionModelBase Version { get; set; } = version;

    partial void OnVersionChanged(InstancePackageVersionModelBase value) =>
        entry.Purl = PackageHelper.ToPurl(label,
            @namespace,
            projectId,
            value is InstancePackageVersionModel v ? v.Id : null);

    #endregion
}