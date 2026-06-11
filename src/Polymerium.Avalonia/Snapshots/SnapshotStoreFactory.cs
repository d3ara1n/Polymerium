using System;
using System.IO;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.Avalonia.Snapshots;

public class SnapshotStoreFactory(IServiceProvider provider) : ISnapshotStoreFactory
{
    public ISnapshotStore Open(string key)
    {
        var dir = PathDef.Default.DirectoryOfSnapshots(key);
        var path = Path.Combine(dir, "snapshots.sqlite.db");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var freeSql = new FreeSqlBuilder()
                     .UseConnectionString(DataType.Sqlite, $"Data Source=\"{path}\";Cache=Private")
                     .UseAutoSyncStructure(true)
                     .Build();
        return ActivatorUtilities.CreateInstance<SnapshotStore>(provider, freeSql);
    }
}
