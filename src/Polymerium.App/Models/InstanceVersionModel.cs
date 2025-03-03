using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using System;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class InstanceVersionModel(
    string label,
    string? @namespace,
    string pid,
    string vid,
    string display,
    ReleaseType releaseType,
    DateTimeOffset updatedAt) : ModelBase
{
    #region Reactive Properties

    [ObservableProperty]
    private bool _isCurrent;

    #endregion

    #region Direct Properties

    public string Label => label;
    public string? Namespace => @namespace;
    public string Pid => pid;
    public string Vid => vid;
    public string Display => display;
    public ReleaseType ReleaseTypeRaw => releaseType;
    public string ReleaseType => releaseType.ToString();
    public DateTimeOffset UpdatedAtRaw => updatedAt;
    public string UpdatedAt { get; } = updatedAt.Humanize();

    #endregion
}