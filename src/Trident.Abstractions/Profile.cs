namespace Trident.Abstractions;

public record Profile(string Version, IList<Profile.Loader> Loaders, IList<Profile.Layer> Attachments)
{
    public record Loader(string Id, string Version)
    {
        public const string COMPONENT_MINECRAFT = "net.minecraft";
        public const string COMPONENT_FORGE = "net.minecraftforge";
        public const string COMPONENT_NEOFORGE = "net.neoforged";
        public const string COMPONENT_FABRIC = "net.fabricmc";
        public const string COMPONENT_QUILT = "org.quiltmc";
        public const string COMPONENT_BUILTIN_STORAGE = "builtin.trident.storage";

        public static readonly string[] ModLoaders =
            { COMPONENT_FORGE, COMPONENT_NEOFORGE, COMPONENT_FABRIC, COMPONENT_QUILT };
    }
    public record Layer(bool Enabled, string Summary, string Source, IList<string> Content);
}