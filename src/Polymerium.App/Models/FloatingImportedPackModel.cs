using Polymerium.App.Facilities;
using Polymerium.App.Properties;
using Trident.Abstractions.Importers;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public class FloatingImportedPackModel(
    string path,
    CompressedProfilePack pack,
    ImportedProfileContainer container) : ModelBase
{
    #region Direct

    public string Path => path;
    public int PackageCount { get; } = container.Profile.Setup.Packages.Count;

    public string LoaderLabel { get; } =
        container.Profile.Setup.Loader != null && LoaderHelper.TryParse(container.Profile.Setup.Loader, out var result)
            ? LoaderHelper.ToDisplayLabel(result.Identity, result.Version)
            : Resources.Enum_None;

    public CompressedProfilePack Pack => pack;
    public ImportedProfileContainer Container => container;

    #endregion
}
