using System.Threading.Tasks;
using CliFx;

namespace Polymerium.Cli;

internal static class Program
{
    private static async Task<int> Main()
    {
        return await new CliApplicationBuilder().AddCommandsFromThisAssembly().Build().RunAsync();
    }
}
