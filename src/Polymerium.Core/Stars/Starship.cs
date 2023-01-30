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
            var match = compiled.Match(x);
            if (match.Success)
            {
                var group = match.Groups["field"];
                if (Cargo.Any(x => x.Label == group.Value))
                {
                    var crate = Cargo.First(x => x.Label == group.Value);
                    var result = x[..match.Index] + crate.Content + x[(match.Index + match.Length)..];
                    return result;
                }

                return x;
            }

            return x;
        });
    }
}