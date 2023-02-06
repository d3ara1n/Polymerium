using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Polymerium.Core.Stars;

public class Starship
{
    private readonly Regex compiled = new(@"\$\{(?<field>[a-z_]+)\}");

    public Starship(IEnumerable<Crate> cargo)
    {
        Cargo = cargo;
    }

    public IEnumerable<Crate> Cargo { get; }

    public IEnumerable<string> Ship(IEnumerable<string> from)
    {
        return from.Select(x =>
        {
            var matches = compiled.Matches(x);
            var mid = x;
            foreach (Match match in matches)
                if (match.Success)
                {
                    var group = match.Groups["field"];
                    if (Cargo.Any(it => it.Label == group.Value))
                    {
                        var crate = Cargo.First(it => it.Label == group.Value);
                        var key = $"${{{crate.Label}}}";
                        mid = mid.Replace(key, crate.Content);
                    }
                }

            return mid;
        });
    }
}