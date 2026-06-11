using System;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.Models;

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
