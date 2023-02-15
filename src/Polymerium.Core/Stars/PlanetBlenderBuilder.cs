using System;
using System.Collections.Generic;
using System.Linq;
using IBuilder;

namespace Polymerium.Core.Stars;

public class PlanetBlenderBuilder : IBuilder<PlanetBlender>
{
    private readonly StarshipBuilder starshipBuilder = new();
    private IEnumerable<string> gameArguments = Enumerable.Empty<string>();
    private string javaPath = string.Empty;
    private IEnumerable<string> jvmArguments = Enumerable.Empty<string>();
    private string mainClass = string.Empty;
    private string workingDirectory = string.Empty;

    public PlanetBlender Build()
    {
        var starship = starshipBuilder.Build();
        var jvm = starship.Ship(jvmArguments);
        var game = starship.Ship(gameArguments);
        var arguments = jvm.Append(mainClass).Concat(game);
        var options = new PlanetOptions
        {
            Arguments = arguments,
            JavaExecutable = javaPath,
            WorkingDirectory = workingDirectory
        };
        return new PlanetBlender(options);
    }

    public PlanetBlenderBuilder ConfigureStarship(Action<StarshipBuilder> configure)
    {
        configure?.Invoke(starshipBuilder);
        return this;
    }

    public PlanetBlenderBuilder WithJavaPath(string javaPath)
    {
        this.javaPath = javaPath;
        return this;
    }

    public PlanetBlenderBuilder WithMainClass(string mainClass)
    {
        this.mainClass = mainClass;
        return this;
    }

    public PlanetBlenderBuilder WithWorkingDirectory(string working)
    {
        workingDirectory = working;
        return this;
    }

    public PlanetBlenderBuilder WithGameArguments(IEnumerable<string> arguments)
    {
        gameArguments = arguments;
        return this;
    }

    public PlanetBlenderBuilder WithJvmArguments(IEnumerable<string> arguments)
    {
        jvmArguments = arguments;
        return this;
    }
}