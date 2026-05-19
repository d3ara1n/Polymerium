using System.IO;
using FreeSql;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Snapshots;

namespace Polymerium.App.Snapshots;

public class SnapshotStoreFactory : ISnapshotStoreFactory
{
    public ISnapshotStore Open(string key)
    {
        var dir = PathDef.Default.DirectoryOfSnapshots(key);
        var path = Path.Combine(dir, "data.shot.db");
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var freeSql = new FreeSqlBuilder()
                     .UseConnectionString(DataType.Sqlite, $"Data Source=\"{path}\";Cache=Private")
                     .UseAutoSyncStructure(true)
                     .Build();
        return new SnapshotStore(freeSql);
    }
}
