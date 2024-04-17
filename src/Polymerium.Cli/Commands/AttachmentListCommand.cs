using Spectre.Console.Cli;
using System.ComponentModel;

namespace Polymerium.Cli.Commands;

[Description("Resolve and list all the attachments")]
public class AttachmentListCommand : Command<AttachmentListCommand.Arguments>
{
    public class Arguments : CommandSettings
    {

    }

    public override int Execute(CommandContext context, Arguments settings)
    {
        return 0;
    }
}