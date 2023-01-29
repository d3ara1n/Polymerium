using System.Collections.Generic;

namespace Polymerium.Core.Stars
{
    public class StarshipBuilder : IBuilder.IBuilder<Starship>
    {
        private readonly List<Crate> crates = new List<Crate>();

        public StarshipBuilder AddCrate(string label, string content)
        {
            var crate = new Crate()
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

        public Starship Build() => new Starship(crates);
    }
}