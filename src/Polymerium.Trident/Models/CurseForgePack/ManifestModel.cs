namespace Polymerium.Trident.Models.CurseForgePack;

public record ManifestModel(
    ManifestModel.MinecraftModel Minecraft,
    string ManifestType,
    int ManifestVersion,
    string Name,
    string Version,
    string Author,
    IReadOnlyList<ManifestModel.FileModel> Files,
    string Overrides)
{
    public record MinecraftModel(string Version, IReadOnlyList<MinecraftModel.ModLoaderModel> ModLoaders)
    {
        public record ModLoaderModel(string Id, bool Primary);
    }

    public record FileModel(uint ProjectId, uint FileId, bool Required);
}