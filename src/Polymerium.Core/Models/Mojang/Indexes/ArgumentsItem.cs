using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Polymerium.Core.Models.Mojang.Converters;

namespace Polymerium.Core.Models.Mojang.Indexes;

[JsonConverter(typeof(ArgumentsItemConverter))]
public struct ArgumentsItem
{
    public IEnumerable<Rule> Rules { get; set; }
    public IEnumerable<string> Values { get; set; }

    public bool Verfy()
    {
        return !(Rules != null && Rules.Any()) && Values != null && Values.Any();
    }
}