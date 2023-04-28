using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Cli.Commands
{
    [Command("instance unlock")]
    internal class InstanceUnlockCommand : ICommand
    {
        [CommandParameter(0)]
        public required string InstanceId { get; set; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            var instances = new InstanceMachine();
            instances.Load(Path.Combine(Environment.CurrentDirectory, "instances.json"));
            throw new NotImplementedException();
        }
    }
}
