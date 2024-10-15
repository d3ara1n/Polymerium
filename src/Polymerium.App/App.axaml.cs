using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Polymerium.App;

public partial class App : Application
{
    private readonly IServiceProvider _provider;

    public App()
    {
        _provider = BuildServices();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = ConstructWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var cfgBuilder = new ConfigurationBuilder();
        var services = new ServiceCollection();

        cfgBuilder.AddJsonFile("appsettings.json", false);
#if DEBUG
        cfgBuilder.AddJsonFile("appsettings.Development.json", true);
#else
        cfgBuilder.AddJsonFile("appsettings.Production.json", true);
#endif
        var configuration = cfgBuilder.Build();
        services.AddSingleton<IConfiguration>(configuration);

        Startup.ConfigureServices(services, configuration);

        return services.BuildServiceProvider();
    }

    private static Window ConstructWindow()
    {
        var window = new Window();
        var shell = new Shell();


        return window;
    }
}