namespace Polymerium.Trident;

public record Profile(string Name, string Author, string Summary, Uri? Thumbnail, string? Reference,
    Profile.MetaData Metadata, IList<Profile.TimelinePoint> Timeline)
{
    public record MetaData(IList<MetaData.Loader> Loaders, IList<MetaData.Layer> Attachments)
    {
        public record Loader(string Id, string Version);

        public record Layer(bool Enabled, string? Source, IList<string> Content);
    };

    public record TimelinePoint();
}