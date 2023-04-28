using CliFx;
using System.Threading.Tasks;

namespace Polymerium.Cli
{
    internal static class Program
    {
        static async Task<int> Main() =>
            await new CliApplicationBuilder().AddCommandsFromThisAssembly().Build().RunAsync();
    }
}
