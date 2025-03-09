using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using NeoSmart.Caching.Sqlite;
using Polymerium.App.Services;
using Polymerium.Trident;
using Polymerium.Trident.Services;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
           .AddAvalonia()
           .AddHttpClient()
           .ConfigureHttpClientDefaults(builder => builder.RemoveAllLoggers())
           .AddLogging(logging => logging
                                 .AddConsole()
                                 .AddDebug()
                                 .AddFilter<ConsoleLoggerProvider>(null, LogLevel.Information)
                                 .AddFilter<DebugLoggerProvider>(null, LogLevel.Debug))
           .AddSqliteCache(setup =>
            {
                setup.MemoryOnly = false;
                var path = PathDef.Default.FileOfPrivateCache;
                var dir = Path.GetDirectoryName(path);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                setup.CachePath = path;
            });

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
           .AddSingleton<OverlayService>();
    }
}