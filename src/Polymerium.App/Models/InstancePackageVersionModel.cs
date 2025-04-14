using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class InstancePackageVersionModel(
    string id,
    string name,
    DateTimeOffset publishedAt,
    ReleaseType releaseType) : InstancePackageVersionModelBase
{
    #region Reactive

    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    #endregion

    #region Direct

    public string Id => id;
    public string Name => name;
    public DateTimeOffset PublishedAtRaw => publishedAt;
    public string PublishedAt => publishedAt.Humanize();

    public ReleaseType ReleaseTypeRaw => releaseType;

    #endregion
}