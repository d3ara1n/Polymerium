using System;
using System.IO;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace Polymerium.Cli.Commands;

[Command("instance unlock")]
internal class InstanceUnlockCommand : ICommand
{
    [CommandParameter(0)] public required string InstanceId { get; set; }

    public ValueTask ExecuteAsync(IConsole console)
    {
        var instances = new InstanceMachine();
        instances.Load(Path.Combine(Environment.CurrentDirectory, "instances.json"));
        throw new NotImplementedException();
    }
}