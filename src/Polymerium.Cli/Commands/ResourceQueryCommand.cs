﻿using Spectre.Console.Cli;
using System.ComponentModel;

namespace Polymerium.Cli.Commands;

[Description("Query a project in the specific repository")]
public class ResourceQueryCommand : Command<ResourceQueryCommand.Arguments>
{
    public override int Execute(CommandContext context, Arguments settings) => throw new NotImplementedException();

    public class Arguments : CommandSettings
    {
    }
}