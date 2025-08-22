using System;
using Avalonia.Media.Imaging;
using Humanizer;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class InstanceActionModel(
    string projectId,
    string projectName,
    string? oldVersionId,
    string? oldVersionName,
    string? newVersionId,
    string? newVersionName,
    Bitmap thumbnail,
    DateTimeOffset modifiedAt,
    bool canUndo) : ModelBase
{
    #region Direct

    public InstanceActionKind Kind
    {
        get
        {
            if (oldVersionId != null && newVersionId != null) return InstanceActionKind.Update;

            if (oldVersionId == null && newVersionId != null) return InstanceActionKind.Add;

            if (oldVersionId != null && newVersionId == null) return InstanceActionKind.Remove;

            return InstanceActionKind.Unknown;
        }
    }

    public string ProjectId => projectId;
    public string ProjectName => projectName;
    public string? OldVersionId => oldVersionId;
    public string? OldVersionName => oldVersionName;
    public string? NewVersionId => newVersionId;
    public string? NewVersionName => newVersionName;
    public Bitmap Thumbnail => thumbnail;

    public DateTimeOffset ModifiedAtRaw => modifiedAt;
    public string ModifiedAt => modifiedAt.Humanize();

    public bool CanUndo => canUndo;

    #endregion
}