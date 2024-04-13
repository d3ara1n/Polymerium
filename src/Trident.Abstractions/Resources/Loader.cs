namespace Trident.Abstractions.Resources
{
    public record Loader(string Identity, string Version)
    {
        public const string COMPONENT_MINECRAFT = "net.minecraft";
        public const string COMPONENT_FORGE = "net.minecraftforge";
        public const string COMPONENT_NEOFORGE = "net.neoforged";
        public const string COMPONENT_FABRIC = "net.fabricmc";
        public const string COMPONENT_QUILT = "org.quiltmc";
        public const string COMPONENT_AUTHLIB_INJECTOR = "moe.yushi.authlib-injector";
        public const string COMPONENT_BUILTIN_STORAGE = "loader.trident.storage";

        public static readonly IDictionary<string, string> MODLOADER_NAME_MAPPINGS =
            new Dictionary<string, string>
            {
                { COMPONENT_FORGE, "Forge" },
                { COMPONENT_NEOFORGE, "NeoForge" },
                { COMPONENT_FABRIC, "Fabric" },
                { COMPONENT_QUILT, "Quilt" }
            };

        public string Identity { get; set; } = Identity;
        public string Version { get; set; } = Version;
    }
}