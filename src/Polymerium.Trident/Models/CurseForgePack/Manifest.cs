namespace Polymerium.Trident.Models.CurseForgePack;

public record Manifest(
    Manifest.MinecraftModel Minecraft,
    string ManifestType,
    int ManifestVersion,
    string Name,
    string Version,
    string Author,
    IReadOnlyList<Manifest.FileModel> Files,
    string Overrides)
{
    #region Nested type: FileModel

    public record FileModel(uint ProjectId, uint FileId, bool Required);

    #endregion

    #region Nested type: MinecraftModel

    public record MinecraftModel(string Version, IReadOnlyList<MinecraftModel.ModLoaderModel> ModLoaders)
    {
        #region Nested type: ModLoaderModel

        public record ModLoaderModel(string Id, bool Primary);

        #endregion
    }

    #endregion
}