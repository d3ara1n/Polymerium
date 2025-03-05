using Polymerium.App.Facilities;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public class FloatingImportedPackModel(CompressedProfilePack pack, ImportedProfileContainer container) : ModelBase
{
    #region Direct

    public int PackageCount { get; } = container.Profile.Setup.Stage.Count + container.Profile.Setup.Stash.Count;

    public string LoaderLabel { get; } =
        container.Profile.Setup.Loader != null && LoaderHelper.TryParse(container.Profile.Setup.Loader, out var result)
            ? LoaderHelper.ToDisplayLabel(result.Identity, result.Version)
            : "None";

    public CompressedProfilePack Pack => pack;
    public ImportedProfileContainer Container => container;

    #endregion
}