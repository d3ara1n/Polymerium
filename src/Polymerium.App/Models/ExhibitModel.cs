using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public partial class ExhibitModel(
    string label,
    string? ns,
    string projectId,
    string projectName,
    string summary,
    Uri thumbnail,
    string author,
    IReadOnlyList<string> tags,
    DateTimeOffset updatedAt,
    ulong downloads,
    Uri reference) : ModelBase
{
    #region Direct

    public string Label => label;

    public string? Ns => ns;

    public string ProjectId => projectId;

    public string ProjectName => projectName;

    public string Summary => summary;

    public Uri Thumbnail => thumbnail;

    public string Author => author;

    public IReadOnlyList<string> Tags => tags;

    public DateTimeOffset UpdatedAtRaw => updatedAt;

    public string UpdatedAt => updatedAt.Humanize();
    public ulong DownloadsRaw => downloads;

    public string Downloads => ((double)downloads).ToMetric(decimals: 2);

    public Uri Reference => reference;

    public ObservableCollection<ExhibitDependencyModel> AttachedDependencies { get; } = [];

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial ExhibitState? State { get; set; }

    [ObservableProperty]
    public partial Profile.Rice.Entry? Installed { get; set; }

    [ObservableProperty]
    public partial string? InstalledVersionName { get; set; }

    [ObservableProperty]
    public partial string? InstalledVersionId { get; set; }

    [ObservableProperty]
    public partial string? PendingVersionName { get; set; }

    [ObservableProperty]
    public partial string? PendingVersionId { get; set; }

    #endregion
}