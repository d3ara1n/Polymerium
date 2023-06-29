using IBuilder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Stars;

public class PlanetaryEngineBuilder : IBuilder<PlanetaryEngine>
{
    private readonly StarshipBuilder starshipBuilder = new();
    private IEnumerable<string> gameArguments = Enumerable.Empty<string>();
    private string javaPath = string.Empty;
    private IEnumerable<string> jvmArguments = Enumerable.Empty<string>();
    private string mainClass = string.Empty;
    private PlanetaryEngineMode mode = PlanetaryEngineMode.FireAndForget;
    private string workingDirectory = string.Empty;

    public PlanetaryEngine Build()
    {
        var starship = starshipBuilder.Build();
        var jvm = starship.Ship(jvmArguments);
        var game = starship.Ship(gameArguments);
        var arguments = jvm.Append(mainClass).Concat(game);
        var options = new PlanetaryEngineOptions
        {
            Arguments = arguments,
            JavaExecutable = javaPath,
            WorkingDirectory = workingDirectory,
            Mode = mode
        };
        return new PlanetaryEngine(options);
    }

    public PlanetaryEngineBuilder CraftStarship(Action<StarshipBuilder> builder)
    {
        builder?.Invoke(starshipBuilder);
        return this;
    }

    public PlanetaryEngineBuilder WithJavaPath(string javaPath)
    {
        this.javaPath = javaPath;
        return this;
    }

    public PlanetaryEngineBuilder WithMainClass(string mainClass)
    {
        this.mainClass = mainClass;
        return this;
    }

    public PlanetaryEngineBuilder WithWorkingDirectory(string working)
    {
        workingDirectory = working;
        return this;
    }

    public PlanetaryEngineBuilder WithGameArguments(IEnumerable<string> arguments)
    {
        gameArguments = arguments;
        return this;
    }

    public PlanetaryEngineBuilder WithJvmArguments(IEnumerable<string> arguments)
    {
        jvmArguments = arguments;
        return this;
    }

    public PlanetaryEngineBuilder AsMode(PlanetaryEngineMode asMode)
    {
        mode = asMode;
        return this;
    }
}
