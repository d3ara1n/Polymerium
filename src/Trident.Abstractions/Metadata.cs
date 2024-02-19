using Trident.Abstractions.Resources;

namespace Trident.Abstractions
{
    public record Metadata(string Version, IList<Metadata.Layer> Layers)
    {
        public string Version { get; set; } = Version;

        public record Layer(
            Attachment? Source,
            bool Enabled,
            string Summary,
            IList<Loader> Loaders,
            IList<Attachment> Attachments)
        {
            public Attachment? Source { get; set; } = Source;
            public bool Enabled { get; set; } = Enabled;
            public string Summary { get; set; } = Summary;
        }
    }
}