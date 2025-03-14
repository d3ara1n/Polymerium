﻿using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using NeoSmart.Caching.Sqlite;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using Trident.Abstractions;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration _, IHostEnvironment environment)
    {
        services
           .AddAvalonia()
           .AddHttpClient()
           .ConfigureHttpClientDefaults(builder => builder.RemoveAllLoggers())
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
                var path = PathDef.Default.FileOfPrivateCache(Program.Brand);
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                setup.CachePath = path;
            })
           .AddMemoryCache();

        // Trident
        services
           .AddCurseForge()
           .AddPrismLauncher()
           .AddMojangLauncher()
           .AddSingleton<ProfileManager>()
           .AddSingleton<RepositoryAgent>()
           .AddSingleton<ImporterAgent>()
           .AddSingleton<InstanceManager>();


        // App
        services
           .AddViewFacilities()
           .AddSingleton<ConfigurationService>()
           .AddSingleton<NotificationService>()
           .AddSingleton<NavigationService>()
           .AddSingleton<OverlayService>()
           .AddSingleton<DataService>()
           .AddSingleton<StateService>();
    }
}