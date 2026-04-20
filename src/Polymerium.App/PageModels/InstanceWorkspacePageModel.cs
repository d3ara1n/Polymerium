using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Trident.Abstractions;
using Trident.Core.Services;

namespace Polymerium.App.PageModels;

public class InstanceWorkspacePageModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(bag, instanceManager, profileManager)
{
    #region Reactive

    public ObservableCollection<object> Changes { get; } = [];

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync() => base.OnInitializeAsync();

    protected override Task OnDeinitializeAsync() => base.OnDeinitializeAsync();

    #endregion

    #region Other

    private void GenerateChangeList()
    {
        // live / import 的变更
        // 由于 import -> live 是全量文件复制，live 的文件只会比 import 多（但存在 .keep 映射了目录时，游戏会往目录里塞东西）
        // 当 live 比 import 多时需要同步，但当 live 比 import 少（比如新增的没复制过来）就不需要在乎

        var liveDir = PathDef.Default.DirectoryOfLive(Basic.Key);
        var importDir = PathDef.Default.DirectoryOfImport(Basic.Key);

        var liveEntries = ScanFolder(liveDir);

        var changes = new List<WorkspaceChangeModel>();
        foreach (var liveEntry in liveEntries)
        {
            var livePath = Path.Combine(liveDir, liveEntry);
            var importPath = Path.Combine(importDir, liveEntry);
            var kind = Diff(livePath, importPath);
            if (kind != WorkspaceChangeKind.Same)
            {
                changes.Add(new() { RelativePath = liveEntry, Name = Path.GetFileName(livePath) });
            }
        }
    }

    #endregion

    private IReadOnlyList<string> ScanFolder(string folder)
    {
        var queue = new Queue<DirectoryInfo>();
        var rv = new List<string>();
        queue.Enqueue(new(folder));
        while (queue.TryDequeue(out var dir))
        {
            if (!dir.Exists)
            {
                continue;
            }

            var files = dir.GetFiles();
            rv.AddRange(files.Select(x => x.Name));
            var dirs = dir.GetDirectories();
            foreach (var d in dirs)
            {
                queue.Enqueue(d);
            }
        }

        return rv;
    }

    private WorkspaceChangeKind Diff(string live, string import)
    {
        // 这里使用 mtime 而不是哈希，live 的文件是从 import 复制的，atime, ctime, mtime 都是相同的
        if (File.Exists(import))
        {
            var ltime = File.GetLastWriteTimeUtc(live);
            var itime = File.GetLastWriteTimeUtc(import);

            if (ltime > itime)
            {
                return WorkspaceChangeKind.Updated;
            }
            else if (ltime < itime)
            {
                return WorkspaceChangeKind.Outupdated;
            }
            else
            {
                return WorkspaceChangeKind.Same;
            }
        }

        return WorkspaceChangeKind.Deleted;
    }
}
