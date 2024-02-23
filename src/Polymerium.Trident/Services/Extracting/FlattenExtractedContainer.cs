using System.IO.Compression;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services.Extracting
{
    public class FlattenExtractedContainer(
        ExtractedContainer original,
        IEnumerable<FlattenContainedLayer> layers,
        (Project, Project.Version)? reference)
    {
        public ExtractedContainer Original { get; } = original;
        public IEnumerable<FlattenContainedLayer> Layers { get; } = layers;
        public (Project, Project.Version)? Reference { get; } = reference;

        public static FlattenExtractedContainer FromExtracted(ExtractedContainer extracted, ZipArchive archive,
            (Project, Project.Version)? reference)
        {
            List<FlattenContainedLayer> layers = new();
            foreach (ContainedLayer layer in extracted.Layers)
            {
                List<SolidFile> files = new();
                if (layer.OverrideDirectoryName != null)
                {
                    foreach (ZipArchiveEntry entry in archive.Entries.Where(x =>
                                 x.FullName.StartsWith(layer.OverrideDirectoryName) && !string.IsNullOrEmpty(x.Name)))
                    {
                        if (entry.Length > 1024 * 1024 * 64)
                        {
                            throw new OverflowException(
                                $"Entry is too big(64MB, got {entry.Length} bytes) for in-memory file buffer");
                        }

                        using Stream stream = entry.Open();
                        byte[] buffer = new byte[entry.Length];
                        using MemoryStream reader = new(buffer);
                        stream.CopyTo(reader);
                        string relative = entry.FullName[layer.OverrideDirectoryName.Length..];
                        if (relative.StartsWith('/') || relative.StartsWith('\\'))
                        {
                            relative = relative.Substring(1);
                        }

                        files.Add(new SolidFile(relative,
                            new ReadOnlyMemory<byte>(buffer)));
                    }
                }

                layers.Add(new FlattenContainedLayer(layer, files));
            }

            return new FlattenExtractedContainer(extracted, layers, reference);
        }
    }
}