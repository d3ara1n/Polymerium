using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class AssetIdentificationModel(
    AssetIdentificationPackageModel? package,
    AssetIdentificationPersistModel persist
) : ModelBase
{
    #region Direct

    public AssetIdentificationPackageModel? Package => package;

    public AssetIdentificationPersistModel Persist => persist;

    #endregion
}
