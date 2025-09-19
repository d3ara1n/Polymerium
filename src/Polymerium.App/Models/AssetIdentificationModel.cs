using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public partial class AssetIdentificationModel(
    AssetIdentificationPackageModel? package,
    AssetIdentificationPersistModel persist) : ModelBase
{
    #region Direct

    public AssetIdentificationPackageModel? Package => package;

    public AssetIdentificationPersistModel Persist => persist;

    #endregion
}
