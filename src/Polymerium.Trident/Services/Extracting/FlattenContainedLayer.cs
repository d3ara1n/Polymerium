using Trident.Abstractions.Extractors;

namespace Polymerium.Trident.Services.Extracting
{
    public class FlattenContainedLayer(ContainedLayer original, IEnumerable<SolidFile> solidFiles)
    {
        public ContainedLayer Original { get; } = original;
        public IEnumerable<SolidFile> SolidFiles { get; } = solidFiles;
    }
}