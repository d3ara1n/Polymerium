using Spectre.Console.Cli;
using System.ComponentModel;

namespace Polymerium.Cli.Commands;

[Description("Resolve/verify a package in the specific repository")]
public class ResourceResolveCommand : Command<ResourceResolveCommand.Arguments>
{
    public class Arguments : CommandSettings { }

    public override int Execute(CommandContext context, Arguments settings) => throw new NotImplementedException();
}