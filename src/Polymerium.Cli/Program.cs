using CliFx;
using System.Threading.Tasks;

namespace Polymerium.Cli;

internal static class Program
{
    private static async Task<int> Main()
    {
        return await new CliApplicationBuilder().AddCommandsFromThisAssembly().Build().RunAsync();
    }
}
