using Polymerium.Trident.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Polymerium.Cli.Commands
{
    public sealed class ListCommand(TridentContext trident) : Command<DummyArguments>
    {
        public override int Execute(CommandContext context, DummyArguments settings)
        {
            var home = new DirectoryInfo(trident.InstanceDir);
            var files = (home.Exists ? home.GetFiles("*.json") : Enumerable.Empty<FileInfo>()).ToArray();
            AnsiConsole.Write(new Rule("[bold yellow]INSTANCES[/]"));
            foreach (var file in files)
                AnsiConsole.WriteLine(Path.GetFileNameWithoutExtension(file.Name));
            

            if (files.Length == 0)
                AnsiConsole.Write(new Rule("[gray]EMPTY[/]").NoBorder());

            return 0;
        }
    }
}