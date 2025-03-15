using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Polymerium.App.Services;

public class AvaloniaLifetime : IHostLifetime
{
    private readonly IHostApplicationLifetime _parent;
    private readonly Thread _thread;

    public AvaloniaLifetime(
        IHostApplicationLifetime parent,
        ILogger<AvaloniaLifetime> logger,
        IHostEnvironment environment)
    {
        _parent = parent;

        logger.LogInformation("""
                              {}({}):{}
                              Polymerium/{}
                              Avalonia/{}
                              """,
                              environment.ApplicationName,
                              environment.EnvironmentName,
                              environment.ContentRootPath,
                              typeof(AvaloniaLifetime).Assembly.GetName().Version,
                              typeof(AvaloniaObject).Assembly.GetName().Version);

        if (OperatingSystem.IsWindows())
        {
            _thread = new Thread(Serve) { Name = "Avalonia Lifetime" };
            _thread.SetApartmentState(ApartmentState.STA);
        }
        else
        {
            _thread = new Thread(Deserve);
        }
    }

    private void Serve()
    {
        Program.BuildAvaloniaApp().StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
        _parent.StopApplication();
    }

    private void Deserve() { }

    #region IHostLifetime Members

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        _thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        // 因为停止 Host 的唯一方法就是 Avalonia 自己退出，所以这里不需要再返去请求 Avalonia 退出（主要是 Avalonia 没给方法来判断其是否已经 Shutdown）
        Task.CompletedTask;

    #endregion
}