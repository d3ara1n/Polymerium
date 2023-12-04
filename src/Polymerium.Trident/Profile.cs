using DotNext;

namespace Polymerium.Trident;

public record Profile(string Name, string Author, string Summary, Uri? Thumbnail, string? Reference,
    Profile.MetaData Metadata, IList<Profile.TimelinePoint> Timeline)
{
    public record MetaData(string Version, IList<MetaData.Loader> Loaders, IList<MetaData.Layer> Attachments)
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

        public record Layer(bool Enabled, string Summary, string? Source, IList<string> Content);
    }

    public record TimelinePoint(string Source, TimelinePoint.ActionKind Kind, DateTimeOffset BeginTime,
        DateTimeOffset EndTime, bool Success)
    {
        public enum ActionKind
        {
            Create,
            Update,
            Restore,
            Play
        }
    }
}