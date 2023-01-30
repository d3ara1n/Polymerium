using Newtonsoft.Json;
using Polymerium.Core.Models.Mojang.Converters;

namespace Polymerium.Core.Models.Mojang.Indexes;

[JsonConverter(typeof(RuleFeaturesConverter))]
public struct RuleFeatures
{
    public string Key { get; set; }
    public bool Enabled { get; set; }
}