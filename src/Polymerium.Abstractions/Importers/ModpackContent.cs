using System;
using System.Collections.Generic;
using Polymerium.Abstractions.Meta;

namespace Polymerium.Abstractions.Importers;

public struct ModpackContent
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public Uri? ThumbnailFile { get; set; }
    public Uri? ReferenceSource { get; set; }
    public GameMetadata Metadata { get; set; }
    public IEnumerable<PackedSolidFile> Files { get; set; }
}