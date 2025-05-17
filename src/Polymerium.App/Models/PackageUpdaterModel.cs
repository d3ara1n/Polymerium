using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class PackageUpdaterModel(
    InstancePackageModel entry,
    Package package,
    Uri thumbnail,
    string oldVersionId,
    string oldVersionName,
    DateTimeOffset oldVersionTime,
    string newVersionId,
    string newVersionName,
    DateTimeOffset newVersionTime) : ModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsChecked { get; set; } = true;

    #endregion

    #region Direct

    public InstancePackageModel Entry => entry;
    public Package Package => package;
    public Uri Thumbnail => thumbnail;
    public string OldVersionId => oldVersionId;
    public string OldVersionName => oldVersionName;
    public DateTimeOffset OldVersionTimeRaw => oldVersionTime;
    public string OldVersionTime => oldVersionTime.Humanize();
    public string NewVersionId => newVersionId;
    public string NewVersionName => newVersionName;
    public DateTimeOffset NewVersionTimeRaw => newVersionTime;
    public string NewVersionTime => newVersionTime.Humanize();

    #endregion
}