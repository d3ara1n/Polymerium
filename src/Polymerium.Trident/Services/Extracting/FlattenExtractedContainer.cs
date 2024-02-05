﻿using System.IO.Compression;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Services.Extracting;

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
        var layers = new List<FlattenContainedLayer>();
        foreach (var layer in extracted.Layers)
        {
            var files = new List<SolidFile>();
            if (layer.OverrideDirectoryName != null)
                foreach (var entry in archive.Entries.Where(x =>
                             x.FullName.StartsWith(layer.OverrideDirectoryName) && !string.IsNullOrEmpty(x.Name)))
                {
                    if (entry.Length > 1024 * 1024 * 16)
                        throw new OverflowException(
                            $"Entry is too big(16MB, got {entry.Length} bytes) for in-memory file buffer");
                    using var stream = entry.Open();
                    var buffer = new byte[entry.Length];
                    _ = entry.Length != stream.Read(buffer, 0, (int)entry.Length);
                    var relative = entry.FullName[layer.OverrideDirectoryName.Length..];
                    if (relative.StartsWith('/') || relative.StartsWith('\\'))
                        relative = relative.Substring(1);
                    files.Add(new SolidFile(relative,
                        new ReadOnlyMemory<byte>(buffer)));
                }

            layers.Add(new FlattenContainedLayer(layer, files));
        }

        return new FlattenExtractedContainer(extracted, layers, reference);
    }
}