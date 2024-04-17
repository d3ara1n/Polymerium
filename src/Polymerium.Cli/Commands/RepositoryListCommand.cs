using Polymerium.Trident.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Polymerium.Cli.Commands;

[Description("List all the repositories by labels")]
public class RepositoryListCommand(RepositoryAgent agent) : Command<DummyArguments>
{
    public override int Execute(CommandContext context, DummyArguments settings)
    {
        AnsiConsole.Write(new Rule("[bold yellow]REPOSITORIES[/]"));
        foreach (var repository in agent.Repositories)
        {
            AnsiConsole.Write(new Text($"{repository.Label}\n"));
        }

        if (!agent.Repositories.Any())
            AnsiConsole.Write(new Rule("[gray]EMPTY[/]").NoBorder());

        return 0;
    }
}