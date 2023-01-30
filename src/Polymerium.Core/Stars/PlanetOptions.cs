using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Stars
{
    public struct PlanetOptions
    {
        public string JavaExecutable { get; init; }
        public IEnumerable<string> Arguments { get; init; }
        public string WorkingDirectory { get; init; }
    }
}