using System;
using Polymerium.Core.Stars;

namespace Polymerium.Core.Managers;

public class GameManager
{
    public void LaunchFireForget(PlanetaryEngineBuilder builder)
    {
        var blender = builder.Build();
        blender.LaunchFireForget();
    }

    public void LaunchManaged(PlanetaryEngineBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void Launch(PlanetaryEngineBuilder builder, PlanetaryEngineMode mode)
    {
        switch (mode)
        {
            case PlanetaryEngineMode.FireAndForget:
                LaunchFireForget(builder);
                break;
            case PlanetaryEngineMode.Managed:
                LaunchManaged(builder);
                break;
        }
    }
}