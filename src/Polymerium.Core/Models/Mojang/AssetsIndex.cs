using System.Collections.Generic;
using Newtonsoft.Json;
using Polymerium.Core.Models.Mojang.Converters;

namespace Polymerium.Core.Models.Mojang;

[JsonConverter(typeof(AssetsIndexConverter))]
public struct AssetsIndex
{
    public IEnumerable<AssetsIndexItem> Objects { get; set; }
}