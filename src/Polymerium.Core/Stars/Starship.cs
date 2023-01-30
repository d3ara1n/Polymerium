using Polymerium.Core.Models.Mojang.Indexes;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Polymerium.Core.Stars
{
    public class Starship
    {
        private Regex compiled = new Regex(@"\$\{(?<field>[a-z_]+)\}");
        public IEnumerable<Crate> Cargo { get; private set; }

        public Starship(IEnumerable<Crate> cargo)
        {
            Cargo = cargo;
        }

        public IEnumerable<string> Ship(IEnumerable<ArgumentsItem> from)
        {
            return from.Where(x => x.Verfy()).SelectMany(x => x.Values.Select(x =>
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
                    else
                    {
                        return x;
                    }
                }
                else
                {
                    return x;
                }
            }));
        }
    }
}