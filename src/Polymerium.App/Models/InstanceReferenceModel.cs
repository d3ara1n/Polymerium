using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class InstanceReferenceModel : ModelBase
{
    public partial class VersionModel(string name, ReleaseType type, DateTimeOffset updatedAt) : ModelBase
    {
        #region Direct Properties

        public string Name = name;
        public DateTimeOffset UpdatedAtRaw => updatedAt;
        public ReleaseType Type => type;
        public string TypeName = type.ToString();
        public string UpdatedAt => updatedAt.Humanize();

        #endregion

        #region Reactive Properties

        [ObservableProperty] private bool _isCurrent;

        #endregion
    }

    #region Reactive Properties

    [ObservableProperty] private string? _name;
    [ObservableProperty] private Uri? _thumbnail;
    [ObservableProperty] private IReadOnlyList<VersionModel>? _versions;
    [ObservableProperty] private string? _sourceLabel;
    [ObservableProperty] private Uri? _sourceUrl;

    #endregion
}