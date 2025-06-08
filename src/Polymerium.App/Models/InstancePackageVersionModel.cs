using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class InstancePackageVersionModel(
    string id,
    string name,
    string compatibleLoaders,
    string compatibleVersions,
    DateTimeOffset publishedAt,
    ReleaseType releaseType,
    IReadOnlyList<Dependency> dependencies) : InstancePackageVersionModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    #endregion

    #region Direct

    public string Id => id;
    public string Name => name;
    public string CompatibleLoaders => compatibleLoaders;
    public string CompatibleVersions => compatibleVersions;
    public DateTimeOffset PublishedAtRaw => publishedAt;
    public string PublishedAt => publishedAt.Humanize();

    public ReleaseType ReleaseTypeRaw => releaseType;

    public IReadOnlyList<Dependency> Dependencies => dependencies;

    #endregion
}