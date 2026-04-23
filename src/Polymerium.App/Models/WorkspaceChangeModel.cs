using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class WorkspaceChangeModel : ModelBase
{
    #region Direct

    public required string RelativePath { get; set; }
    public required string FileName { get; set; }
    public required string LivePath { get; set; }
    public required string ImportPath { get; set; }
    public required string FileType { get; set; }
    public required long FileSizeRaw { get; set; }
    public required DateTime FileLastModifiedRaw { get; set; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial WorkspaceChangeKind Kind { get; set; }

    #endregion
}
