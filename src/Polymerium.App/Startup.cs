using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using MirrorChyan.Net;
using NeoSmart.Caching.Sqlite;
using Polly;
using Polymerium.App.Models;
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
using VelopackExtension.MirrorChyan;

namespace Polymerium.App;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration _, IHostEnvironment environment)
    {
        services
           .AddSentry()
           .AddAvalonia()
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
                                                                                   new
                                                                                       Uri($"socks4://{config.NetworkProxyAddress}:{config.NetworkProxyPort}"),
                                                                               ProxyProtocol.Socks5 =>
                                                                                   new
                                                                                       Uri($"socks5://{config.NetworkProxyAddress}:{config.NetworkProxyPort}"),
                                                                               _ => new
                                                                                   Uri($"http://{config.NetworkProxyAddress}:{config.NetworkProxyPort}")
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
           .AddSingleton<ScrapService>()
           .AddSingleton<InstanceService>()
           .AddSingleton<WidgetHostService>();
    }
}
