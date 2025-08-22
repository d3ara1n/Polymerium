using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Polymerium.App.Services;

public class AvaloniaLifetime : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Thread _thread;

    public AvaloniaLifetime(
        IHostApplicationLifetime lifetime,
        ILogger<AvaloniaLifetime> logger,
        IHostEnvironment environment)
    {
        _lifetime = lifetime;

        logger.LogInformation("""
                              {}({}):{}
                              Polymerium/{}
                              Avalonia({})/{}
                              """,
            environment.ApplicationName,
            environment.EnvironmentName,
            environment.ContentRootPath,
            typeof(AvaloniaLifetime).Assembly.GetName().Version,
            Program.Debug ? "Debug" : "Prod",
            typeof(AvaloniaObject).Assembly.GetName().Version);

        if (OperatingSystem.IsWindows())
        {
            _thread = new(Serve) { Name = "Avalonia Lifetime" };
            _thread.SetApartmentState(ApartmentState.STA);
        }
        else if (OperatingSystem.IsLinux())
        {
            _thread = new(Serve) { Name = "Avalonia Lifetime" };
        }
        else
        {
            throw new NotSupportedException("Unsupported platform");
        }
    }

    #region IHostedService Members

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #endregion

    private void Serve()
    {
        Program.BuildAvaloniaApp().StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
        _lifetime.StopApplication();
    }

    private void Deserve()
    {
    }
}