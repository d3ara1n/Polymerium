using Avalonia;
using System;
using Microsoft.Extensions.Hosting;

namespace Polymerium.App;

class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        Startup.ConfigureServices(builder.Services, builder.Configuration);
        var host = builder.Build();
        host.Run();
    }
}