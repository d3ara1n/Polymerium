using System.Collections.Generic;
using IBuilder;

namespace Polymerium.Core.Stars;

public class StarshipBuilder : IBuilder<Starship>
{
    private readonly List<Crate> crates = new();

    public Starship Build()
    {
        return new Starship(crates);
    }

    public StarshipBuilder AddCrate(string label, string content)
    {
        var crate = new Crate
        {
            Label = label,
            Content = content
        };
        return AddCrate(crate);
    }

    public StarshipBuilder AddCrate(Crate crate)
    {
        crates.Add(crate);
        return this;
    }

    public StarshipBuilder AddCargo(IEnumerable<Crate> cargo)
    {
        foreach (var crate in cargo) crates.Add(crate);

        return this;
    }

    public StarshipBuilder AddCargo(IDictionary<string, string> cargo)
    {
        foreach (var (label, content) in cargo) AddCrate(label, content);

        return this;
    }
}