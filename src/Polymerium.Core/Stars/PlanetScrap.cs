using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Stars
{
    public struct PlanetScrap
    {
        public ulong Index { get; init; }
        public PlanetScrapSeverity Severity { get; init; }
        public string Line { get; init; }
        public string? Source { get; init; }
    }
}
