using System;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.GameAssets;

public struct AssetProduct
{
    public ResourceType Type { get; set; }
    public Uri FileName { get; set; }
    public string Name { get; set; }
    public string? Version { get; set; }
    public string? Description { get; set; }
}
