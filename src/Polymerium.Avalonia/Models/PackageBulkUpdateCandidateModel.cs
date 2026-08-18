using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

public partial class PackageBulkUpdateCandidateModel(
    InstancePackageModel model,
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
    public partial PackageBulkUpdateDecision Decision { get; set; } = PackageBulkUpdateDecision.Update;

    #endregion

    #region Direct

    public InstancePackageModel Model => model;
    public Package Package => package;
    public Uri Thumbnail => thumbnail;
    public string OldVersionId => oldVersionId;
    public string OldVersionName => oldVersionName;
    public DateTimeOffset OldVersionTime => oldVersionTime;
    public string NewVersionId => newVersionId;
    public string NewVersionName => newVersionName;
    public DateTimeOffset NewVersionTime => newVersionTime;

    #endregion
}
