using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trident.Abstractions;

namespace Polymerium.App.Services;

public class AvaloniaLifetime : IHostedService
{
    private readonly ConfigurationService _configuration;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly Thread _thread;

    public AvaloniaLifetime(
        IHostApplicationLifetime lifetime,
        ILogger<AvaloniaLifetime> logger,
        IHostEnvironment environment,
        ConfigurationService configuration)
    {
        _lifetime = lifetime;
        _configuration = configuration;

        logger.LogInformation("""
                              {app}({env}):{root}
                              Polymerium/{app_version}
                              Avalonia({debug})/{ava_version}
                              Home: {home}
                              """,
                              environment.ApplicationName,
                              environment.EnvironmentName,
                              environment.ContentRootPath,
                              typeof(AvaloniaLifetime).Assembly.GetName().Version,
                              Program.Debug ? "Debug" : "Prod",
                              typeof(AvaloniaObject).Assembly.GetName().Version,
                              PathDef.Default.Home);

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

    private static CultureInfo GetSafeCultureInfo(string cultureName)
    {
        try
        {
            return CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException)
        {
            // 回退到英语（美国）
            return CultureInfo.GetCultureInfo("en-US");
        }
        catch (ArgumentException)
        {
            // 处理无效的文化名称参数
            return CultureInfo.GetCultureInfo("en-US");
        }
    }

    private void Serve()
    {
        CultureInfo.CurrentUICulture = GetSafeCultureInfo(_configuration.Value.ApplicationLanguage);
        Properties.Resources.Culture = CultureInfo.CurrentUICulture;
        Program.BuildAvaloniaApp().StartWithClassicDesktopLifetime(Environment.GetCommandLineArgs());
        _lifetime.StopApplication();
    }

    private void Deserve() { }
}
