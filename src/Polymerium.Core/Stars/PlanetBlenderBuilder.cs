using Polymerium.Core.Models.Mojang.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Polymerium.Core.Stars
{
    public class PlanetBlenderBuilder : IBuilder.IBuilder<PlanetBlender>
    {
        private StarshipBuilder starshipBuilder = new();
        private string javaPath;
        private string mainClass;
        private string workingDirectory;
        private IEnumerable<ArgumentsItem> gameArguments;
        private IEnumerable<ArgumentsItem> jvmArguments;

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

        public PlanetBlenderBuilder WithGameArguments(IEnumerable<ArgumentsItem> arguments)
        {
            gameArguments = arguments;
            return this;
        }

        public PlanetBlenderBuilder WithJvmArguments(IEnumerable<ArgumentsItem> arguments)
        {
            jvmArguments = arguments;
            return this;
        }

        public PlanetBlenderBuilder()
        { }

        public PlanetBlender Build()
        {
            var starship = starshipBuilder.Build();
            var jvm = starship.Ship(jvmArguments);
            var game = starship.Ship(gameArguments);
            var arguments = string.Join(' ', jvm.Append(mainClass).Concat(game));
            var options = new PlanetOptions()
            {
                Arguments = arguments,
                JavaExecutable = javaPath
            };
            return new PlanetBlender(options);
        }
    }
}