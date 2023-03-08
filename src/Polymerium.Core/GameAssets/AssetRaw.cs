using System;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.GameAssets;

public struct AssetRaw
{
    public ResourceType Type { get; set; }
    public Uri FileName { get; set; }
}