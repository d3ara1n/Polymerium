using Polymerium.Abstractions.Resources;
using System;

namespace Polymerium.Core.GameAssets;

public struct AssetRaw
{
    public ResourceType Type { get; set; }
    public Uri FileName { get; set; }
}
