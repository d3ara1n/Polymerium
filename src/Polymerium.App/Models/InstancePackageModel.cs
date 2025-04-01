using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class InstancePackageModel(
    Profile.Rice.Entry entry,
    bool isLocked,
    string name,
    string version,
    string summary,
    Uri? reference,
    Bitmap thumbnail,
    ResourceKind kind) : ModelBase
{
    #region Direct

    public Profile.Rice.Entry Entry => entry;
    public Uri? Reference => reference;
    public string Name => name;
    public string Summary => summary;
    public Bitmap Thumbnail => thumbnail;
    public ResourceKind Kind => kind;

    public MappingCollection<string, string> Tags { get; } = new(entry.Tags, x => x, x => x);

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsLocked { get; set; } = isLocked;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = entry.Enabled;

    partial void OnIsEnabledChanged(bool value) => entry.Enabled = value;

    [ObservableProperty]
    public partial string Version { get; set; } = version;

    #endregion
}