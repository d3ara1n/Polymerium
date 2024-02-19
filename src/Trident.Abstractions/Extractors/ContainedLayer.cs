using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Extractors
{
    public class ContainedLayer
    {
        public string Summary { get; set; } = string.Empty;
        public IList<Loader> Loaders { get; } = new List<Loader>();
        public IList<Attachment> Attachments { get; } = new List<Attachment>();
        public string? OverrideDirectoryName { get; set; }
    }
}