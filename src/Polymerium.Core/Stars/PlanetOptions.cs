using System.Collections.Generic;

namespace Polymerium.Core.Stars;

public struct PlanetOptions
{
    public string JavaExecutable { get; init; }
    public IEnumerable<string> Arguments { get; init; }
    public string WorkingDirectory { get; init; }
}