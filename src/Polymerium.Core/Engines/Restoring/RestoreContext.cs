using Polymerium.Abstractions.Models;

namespace Polymerium.Core.Engines.Restoring;

public class RestoreContext
{
    public RestoreContext(PolylockData polylock)
    {
        Polylock = polylock;
    }

    public PolylockData Polylock { get; }
}