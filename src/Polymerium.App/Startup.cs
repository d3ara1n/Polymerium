using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Huskui.Avalonia.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using NeoSmart.Caching.Sqlite;
using Polly;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Sentry;
using Trident.Abstractions;
using Trident.Abstractions.Exporters;
using Trident.Abstractions.Importers;
using Trident.Core.Exporters;
using Trident.Core.Extensions;
using Trident.Core.Importers;
using Trident.Core.Services;
using VelopackExtension.MirrorChyan;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, bool debug)
    {
        services
           .AddHttpClient()
           .ConfigureHttpClientDefaults(builder => builder
                                                  .RemoveAllLoggers()
                                                  .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                                                   {
                                                       var handler = new HttpClientHandler();

                                                       // Try to get configuration service to apply proxy settings
                                                       var configService = serviceProvider
                                                          .GetService<ConfigurationService>();
                                                       try
                                                       {
                                                           if (configService != null)
                                                           {
                                                               var config = configService.Value;
                                                               var proxyMode = (ProxyMode)config.NetworkProxyMode;

                                                               switch (proxyMode)
                                                               {
                                                                   case ProxyMode.Auto:
                                                                       // Use system proxy (default behavior)
                                                                       handler.UseProxy = true;
                                                                       handler.UseDefaultCredentials = true;
                                                                       break;

                                                                   case ProxyMode.Manual:
                                                                       // Use manually configured proxy
                                                                       if (!string.IsNullOrEmpty(config
                                                                              .NetworkProxyAddress))
                                                                       {
                                                                           var protocol = (ProxyProtocol)config
                                                                              .NetworkProxyProtocol;
                                                                           var proxyUri = protocol switch
                                                                           {
                                                                               ProxyProtocol.Socks4 =>
                                                                                   new($"socks4://{config.NetworkProxyAddress}:{config.NetworkProxyPort}"),
                                                                               ProxyProtocol.Socks5 =>
                                                                                   new($"socks5://{config.NetworkProxyAddress}:{config.NetworkProxyPort}"),
                                                                               _ => new
                                                                                   Uri($"http://{config.NetworkProxyAddress}:{config.NetworkProxyPort}"),
                                                                           };

                                                                           var proxy = new WebProxy(proxyUri);

                                                                           // Set credentials if username is provided
                                                                           if (!string.IsNullOrEmpty(config
                                                                                  .NetworkProxyUsername))
                                                                           {
                                                                               proxy.Credentials =
                                                                                   new NetworkCredential(config
                                                                                          .NetworkProxyUsername,
                                                                                       config.NetworkProxyPassword);
                                                                           }

                                                                           handler.Proxy = proxy;
                                                                           handler.UseProxy = true;
                                                                       }

                                                                       break;

                                                                   case ProxyMode.Disabled:
                                                                       // Direct connection, no proxy
                                                                       handler.UseProxy = false;
                                                                       break;
                                                               }
                                                           }
                                                           else
                                                           {
                                                               // Default: use system proxy
                                                               handler.UseProxy = true;
                                                               handler.UseDefaultCredentials = true;
                                                           }
                                                       }
                                                       catch
                                                       {
                                                           // ignore
                                                       }

                                                       return handler;
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
                                 .AddFilter<ConsoleLoggerProvider>(null, debug ? LogLevel.Debug : LogLevel.Information)
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
           .AddTransient<IProfileImporter, TridentImporter>()
           .AddTransient<IProfileImporter, CurseForgeImporter>()
           .AddTransient<IProfileImporter, ModrinthImporter>()
           .AddTransient<IProfileExporter, TridentExporter>()
           .AddTransient<IProfileExporter, CurseForgeExporter>()
           .AddTransient<IProfileExporter, ModrinthExporter>()
           .AddLifetimeRuntime()
           .AddPrismLauncher()
           .AddMojangLauncher()
           .AddMicrosoft()
           .AddXboxLive()
           .AddMinecraft()
           .AddMclogs()
           .AddSingleton<ProfileManager>()
           .AddSingleton<RepositoryAgent>()
           .AddSingleton<ImporterAgent>()
           .AddSingleton<ExporterAgent>()
           .AddSingleton<InstanceManager>();

        // App
        services
           .AddViewModelActivation<SimpleViewActivator>()
           .AddViewState(builder => builder.WithStatePersistence<SimpleViewStatePersistence>())
           .AddFreeSql()
           .AddMirrorChyan()
           .AddVelopackGithubSource()
           .AddVelopackMirrorChyanSource()
           .AddVelopack()
           .AddTransient<IRepositoryProviderAccessor, BuiltinRepositoryProviderAccessor>()
           .AddTransient<IRepositoryProviderAccessor, UserRepositoryProviderAccessor>()
           .AddSingleton<ConfigurationService>()
           .AddSingleton<NotificationService>()
           .AddSingleton<NavigationService>()
           .AddSingleton<OverlayService>()
           .AddSingleton<DataService>()
           .AddSingleton<PersistenceService>()
           .AddLifetimeService<ScrapService>()
           .AddSingleton<InstanceService>()
           .AddLifetimeService<UpdateService>()
           .AddSingleton<WidgetHostService>();
    }

    public static void InitializeUnhostedServices()
    {
        #region SentrySdk Init (only in Debug)

        if (Program.IsDebug)
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = "https://70f1e791a5f2b8cb31f0947a1bac5e7a@o941379.ingest.us.sentry.io/4510328831410176";
                options.AutoSessionTracking = true;
                options.Environment = Program.IsDebug ? "Development" : "Production";
                options.CacheDirectoryPath = PathDef.Default.PrivateDirectory(Program.Brand);
                options.AddExceptionFilterForType<OperationCanceledException>();
                options.AddExceptionFilterForType<TaskCanceledException>();
                options.SetBeforeSend(@event =>
                {
                    if (@event.Tags.TryGetValue("polymerium.source", out var source))
                    {
                        @event.SetFingerprint("{{ default }}", source);
                    }

                    return @event;
                });
                if (Program.IsDebug)
                {
                    options.Release = "In Dev";
                    options.Debug = true;
                    options.ProfilesSampleRate = 1.0f;
                    options.TracesSampleRate = 1.0f;
                }
                else
                {
                    options.Release = Program.Version;
                    options.ProfilesSampleRate = 0.1f;
                    options.TracesSampleRate = 0.1f;
                }

                options.SendDefaultPii = true;
            });
        }

        #endregion
    }

    public static void DeinitializeUnhostedServices()
    {
        if (Program.IsDebug)
        {
            SentrySdk.Close();
        }
    }
}
