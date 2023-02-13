using System;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Models.Mojang.Indexes;

public struct Library
{
    public LibraryDownloads Downloads { get; set; }
    public string Name { get; set; }
    public LibraryNatives? Natives { get; set; }
    public IEnumerable<Rule> Rules { get; set; }

    public bool Verify()
    {
        return Rules == null
               || !Rules.Any()
               || (
                   Rules
                       .Where(x => x.Action.Equals("allow", StringComparison.OrdinalIgnoreCase))
                       .Any(x => x.Verify())
                   && Rules
                       .Where(x => x.Action.Equals("disallow", StringComparison.OrdinalIgnoreCase))
                       .All(x => x.Verify())
               );
    }
}
