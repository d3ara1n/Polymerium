using Polymerium.Core.Stars;

namespace Polymerium.Core.Extensions;

public static class PlanetaryEngineBuilderExtensions
{
    public static PlanetaryEngineBuilder AsFireForget(this PlanetaryEngineBuilder builder)
    {
        return builder.AsMode(PlanetaryEngineMode.FireAndForget);
    }

    public static PlanetaryEngineBuilder AsManaged(this PlanetaryEngineBuilder builder)
    {
        return builder.AsMode(PlanetaryEngineMode.Managed);
    }
}