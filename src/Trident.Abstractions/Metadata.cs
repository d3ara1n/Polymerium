namespace Trident.Abstractions
{
    public record Metadata(string Version, IList<Metadata.Layer> Layers)
    {
        public record Layer(string? Source, bool Enabled, string Summary, IList<Layer.Loader> Loaders, IList<string> Attachments)
        {

            public record Loader(string Id, string Version)
            {
                public const string COMPONENT_MINECRAFT = "net.minecraft";
                public const string COMPONENT_FORGE = "net.minecraftforge";
                public const string COMPONENT_NEOFORGE = "net.neoforged";
                public const string COMPONENT_FABRIC = "net.fabricmc";
                public const string COMPONENT_QUILT = "org.quiltmc";
                public const string COMPONENT_BUILTIN_STORAGE = "builtin.trident.storage";

                public static readonly IDictionary<string, string> MODLOADER_NAME_MAPPINGS =
                    new Dictionary<string, string>()
                    {
                        {COMPONENT_FORGE, "Forge" },
                        {COMPONENT_NEOFORGE, "NeoForge" },
                        {COMPONENT_FABRIC, "Fabric" },
                        {COMPONENT_QUILT, "Quilt" },
                    };
            }
        }
    }
}
