using System;
using System.IO;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.App.Snapshots;

public class SnapshotStoreFactory(ILogger<SnapshotStoreFactory> logger, IServiceProvider serviceProvider) : ISnapshotStoreFactory
{
    public ISnapshotStore Open(string key)
    {
        var dir = PathDef.Default.DirectoryOfSnapshots(key);
        var path = Path.Combine(dir, "snapshots.sqlite.db");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        logger.LogInformation("Open snapshot store at {Path}", path);
        var freeSql = new FreeSqlBuilder()
                     .UseConnectionString(DataType.Sqlite, $"Data Source=\"{path}\";Cache=Private")
                     .UseAutoSyncStructure(true)
                     .Build();
        return ActivatorUtilities.CreateInstance<SnapshotStore>(serviceProvider, freeSql);
    }
}
