using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models
{
    public partial class InstanceReferenceVersionModel(
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
        public partial bool IsCurrent { get; set; }

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
}
