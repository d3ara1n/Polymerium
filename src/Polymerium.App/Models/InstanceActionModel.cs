using System;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class InstanceActionModel(
    string projectId,
    string projectName,
    string? oldVersionId,
    string? oldVersionName,
    string? newVersionId,
    string? newVersionName,
    DateTimeOffset modifiedAt) : ModelBase
{
    #region Direct

    public InstanceActionKind Kind
    {
        get
        {
            if (oldVersionId != null && newVersionId != null)
                return InstanceActionKind.Unknown;

            if (oldVersionId == null && newVersionId != null)
                return InstanceActionKind.Add;

            if (oldVersionId != null && newVersionId == null)
                return InstanceActionKind.Remove;

            return InstanceActionKind.Unknown;
        }
    }

    #endregion
}