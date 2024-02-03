using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Profiles;

public record Metadata(string Version, IList<Metadata.Layer> Layers)
{
    public string Version { get; set; } = Version;

    public record Layer(
        string? Source,
        bool Enabled,
        string Summary,
        IList<Loader> Loaders,
        IList<string> Attachments)
    {
        public string? Source { get; set; } = Source;
        public bool Enabled { get; set; } = Enabled;
        public string Summary { get; set; } = Summary;
    }
}