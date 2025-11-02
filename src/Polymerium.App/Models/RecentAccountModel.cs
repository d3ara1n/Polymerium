using System;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class RecentAccountModel : ModelBase
{
    #region Direct

    public required string UserName { get; init; }
    public required string TypeName { get; init; }
    public required Uri FaceUrl { get; init; }
    public required string Uuid { get; init; }

    #endregion
}
