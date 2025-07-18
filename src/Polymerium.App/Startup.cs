﻿using System.IO;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using NeoSmart.Caching.Sqlite;
using Polly;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Velopack;
using Velopack.Sources;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration _, IHostEnvironment environment)
    {
        services
           .AddAvalonia()
           .AddHttpClient()
           .ConfigureHttpClientDefaults(builder => builder
                                                  .RemoveAllLoggers()
                                                  .ConfigureHttpClient(client =>
                                                                           client.DefaultRequestHeaders.UserAgent
                                                                              .Add(new ProductInfoHeaderValue(Program
                                                                                      .Brand,
                                                                                   Program.Version)))
                                                  .AddTransientHttpErrorPolicy(configure => configure.RetryAsync()))
           .AddLogging(logging => logging
                                 .AddConsole()
                                 .AddDebug()
                                 .AddFilter<ConsoleLoggerProvider>(null,
                                                                   environment.EnvironmentName == "Development"
                                                                       ? LogLevel.Debug
                                                                       : LogLevel.Information)
                                 .AddFilter<DebugLoggerProvider>(null, LogLevel.Trace))
           .AddSqliteCache(setup =>
            {
                setup.MemoryOnly = false;
                var dir = PathDef.Default.PrivateDirectory(Program.Brand);
                var path = Path.Combine(dir, "cache.sqlite.db");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                setup.CachePath = path;
            })
           .AddMemoryCache();

        // Trident
        services
           .AddCurseForge()
           .AddModrinth()
           .AddPrismLauncher()
           .AddMojangLauncher()
           .AddMicrosoft()
           .AddXboxLive()
           .AddMinecraft()
           .AddSingleton<ProfileManager>()
           .AddSingleton<RepositoryAgent>()
           .AddSingleton<ImporterAgent>()
           .AddSingleton<InstanceManager>();


        // App
        services
           .AddViewFacilities()
           .AddSingleton<MainWindowContext>()
           .AddSingleton<ConfigurationService>()
           .AddSingleton<NotificationService>()
           .AddSingleton<NavigationService>()
           .AddSingleton<OverlayService>()
           .AddSingleton<DataService>()
           .AddSingleton<PersistenceService>()
           .AddSingleton<ScrapService>()
           .AddSingleton<InstanceService>()
           .AddSingleton<WidgetHostService>()
           .AddSingleton<UpdateManager>(_ => new UpdateManager(new GithubSource("https://github.com/d3ara1n/Polymerium",
                                                                                    null,
                                                                                    true)));
    }
}