using System;
using Polymerium.App.Assets;
using Polymerium.App.Facilities;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public class AssetIdentificationPackageModel(Package package) : ModelBase
{
    #region Direct

    public Package Package => package;
    public Uri Thumbnail => package.Thumbnail ?? AssetUriIndex.DirtImage;
    public string Label => package.Label;
    public string ProjectName => package.ProjectName;
    public string VersionName => package.VersionName;
    public Uri Reference => package.Reference;

    #endregion
}
