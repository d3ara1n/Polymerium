namespace Trident.Abstractions.Extractors
{
    public class ExtractedContainer(string name, string version)
    {
        public string Name { get; set; } = name;
        public string Version { get; set; } = version;
        public IList<ContainedLayer> Layers { get; } = new List<ContainedLayer>();
    }
}