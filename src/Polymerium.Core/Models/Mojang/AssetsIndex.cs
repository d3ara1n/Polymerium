using Newtonsoft.Json;
using Polymerium.Core.Models.Mojang.Converters;
using System.Collections.Generic;

namespace Polymerium.Core.Models.Mojang
{
    [JsonConverter(typeof(AssetsIndexConverter))]
    public struct AssetsIndex
    {
        public IEnumerable<AssetsIndexItem> Objects { get; set; }
    }
}