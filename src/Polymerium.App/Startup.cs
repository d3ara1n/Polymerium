using System;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using NeoSmart.Caching.Sqlite;
using Polly;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Abstractions.Exporters;
using Trident.Abstractions.Importers;
using Trident.Core.Exporters;
using Trident.Core.Extensions;
using Trident.Core.Importers;
using Trident.Core.Services;
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
                                                  .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                                                   {
                                                       // TODO: Add User Interface to configure
                                                       UseProxy = true, UseDefaultCredentials = true
                                                   })
                                                  .ConfigureHttpClient(client =>
                                                                           client.DefaultRequestHeaders.UserAgent
                                                                              .Add(new(Program.Brand, Program.Version)))
                                                  .AddTransientHttpErrorPolicy(configure =>
                                                                                   configure.WaitAndRetryAsync(3,
                                                                                       retryAttempt =>
                                                                                           TimeSpan.FromSeconds(Math
                                                                                              .Pow(2,
                                                                                                   retryAttempt)))))
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
                {
                    Directory.CreateDirectory(dir);
                }

                setup.CachePath = path;
            })
           .AddMemoryCache();

        // Trident
        services
           .AddTransient<IProfileImporter, CurseForgeImporter>()
           .AddTransient<IProfileImporter, ModrinthImporter>()
           .AddTransient<IProfileExporter, CurseForgeExporter>()
           .AddTransient<IProfileExporter, ModrinthExporter>()
           .AddPrismLauncher()
           .AddMojangLauncher()
           .AddMicrosoft()
           .AddXboxLive()
           .AddMinecraft()
           .AddSingleton<ProfileManager>()
           .AddSingleton<RepositoryAgent>()
           .AddSingleton<ImporterAgent>()
           .AddSingleton<ExporterAgent>()
           .AddSingleton<InstanceManager>();


        // App
        services
           .AddViewFacilities()
           .AddFreeSql()
           .AddTransient<IRepositoryProviderAccessor, BuiltinRepositoryProviderAccessor>()
           .AddTransient<IRepositoryProviderAccessor, UserRepositoryProviderAccessor>()
           .AddSingleton<ConfigurationService>()
           .AddSingleton<NotificationService>()
           .AddSingleton<NavigationService>()
           .AddSingleton<OverlayService>()
           .AddSingleton<DataService>()
           .AddSingleton<PersistenceService>()
           .AddSingleton<ScrapService>()
           .AddSingleton<InstanceService>()
           .AddSingleton<WidgetHostService>()
           .AddSingleton<UpdateManager>(_ => new(new GithubSource("https://github.com/d3ara1n/Polymerium",
                                                                  null,
                                                                  true)));
    }
}
