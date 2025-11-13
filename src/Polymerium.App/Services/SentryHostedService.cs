using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Sentry;
using Trident.Abstractions;

namespace Polymerium.App.Services;

public class SentryHostedService(IHostEnvironment environment) : IHostedService
{
    #region IHostedService Members

    public Task StartAsync(CancellationToken cancellationToken)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://70f1e791a5f2b8cb31f0947a1bac5e7a@o941379.ingest.us.sentry.io/4510328831410176";
            options.AutoSessionTracking = true;
            options.Environment = environment.EnvironmentName;
            options.CacheDirectoryPath = Path.Combine(PathDef.Default.PrivateDirectory(Program.Brand), "sentry");
            options.AddExceptionFilterForType<OperationCanceledException>();
            if (Program.Debug)
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

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SentrySdk.Close();
        return Task.CompletedTask;
    }

    #endregion
}
