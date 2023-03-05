using System;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.Resources;

public struct RepositoryAssetMeta
{
    public string Id { get; set; }
    public ResourceType Type { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public Uri IconSource { get; set; }

    public string Summary { get; set; }
    //public IEnumerable<string> Versions { get; set; }
    //public Uri BodyUrl { get; set; }
}