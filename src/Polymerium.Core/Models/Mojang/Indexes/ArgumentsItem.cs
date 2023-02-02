using System;
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

    public bool Verify()
    {
        return Rules == null
               || !Rules.Any()
               || (Rules.Where(x => x.Action.Equals("allow", StringComparison.OrdinalIgnoreCase)).Any(x => x.Verify())
                   && Rules.Where(x => x.Action.Equals("disallow", StringComparison.OrdinalIgnoreCase))
                       .All(x => x.Verify()));
    }
}