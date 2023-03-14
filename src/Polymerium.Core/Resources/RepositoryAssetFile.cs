using System;
using System.Collections.Generic;

namespace Polymerium.Core.Resources;

public struct RepositoryAssetFile
{
    public string FileName { get; set; }
    public Uri Source { get; set; }
    public IEnumerable<string> SupportedCoreVersions { get; set; }
    public IEnumerable<string> SupportedModLoaders { get; set; }
    public string? Sha1 { get; set; }
}