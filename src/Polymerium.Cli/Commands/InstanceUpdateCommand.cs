using System;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace Polymerium.Cli.Commands;

[Command("instance update")]
internal class InstanceUpdateCommand : ICommand
{
    public ValueTask ExecuteAsync(IConsole console)
    {
        throw new NotImplementedException();
    }
}
