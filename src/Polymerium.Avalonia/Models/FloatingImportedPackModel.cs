using Polymerium.Avalonia.Facilities;
using TridentCore.Abstractions.Importers;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.Avalonia.Models;

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
            : LanguageManager.Instance.Enum_None.Current();

    public CompressedProfilePack Pack => pack;
    public ImportedProfileContainer Container => container;

    #endregion
}
