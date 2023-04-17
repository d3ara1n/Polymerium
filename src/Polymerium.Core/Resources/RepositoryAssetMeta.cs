using System;
using System.Collections.Generic;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.Resources;

public struct RepositoryAssetMeta
{
    public RepositoryLabel Repository { get; set; }
    public string Id { get; set; }
    public ResourceType Type { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public Uri? IconSource { get; set; }
    public long Downloads { get; set; }
    public Lazy<string>? Description { get; set; }
    public Lazy<IEnumerable<(string, Uri)>>? Screenshots { get; set; }
    public string Summary { get; set; }
    public IEnumerable<string> Versions { get; set; }
}
