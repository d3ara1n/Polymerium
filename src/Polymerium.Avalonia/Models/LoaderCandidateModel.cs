using System;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class LoaderCandidateModel(string id, string display, Uri thumbnail) : ModelBase
{
    #region Direct

    public string Id => id;
    public string Display => display;
    public Uri Thumbnail => thumbnail;

    #endregion
}
