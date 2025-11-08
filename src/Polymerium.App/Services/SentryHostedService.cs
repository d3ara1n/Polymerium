using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Hosting;
using Sentry;

namespace Polymerium.App.Services;

public class SentryHostedService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://70f1e791a5f2b8cb31f0947a1bac5e7a@o941379.ingest.us.sentry.io/4510328831410176";
            options.Release = Program.Debug ? "In Dev" : Program.Version;
            options.AutoSessionTracking = true;
            options.SendDefaultPii = true;
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SentrySdk.Close();
        return Task.CompletedTask;
    }
}
