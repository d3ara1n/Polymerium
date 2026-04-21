using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Trident.Abstractions;
using Trident.Core.Services;

namespace Polymerium.App.PageModels;

public partial class InstanceWorkspacePageModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(bag, instanceManager, profileManager)
{
    #region Reactive

    public ObservableCollection<WorkspaceChangeModel> Changes { get; } = [];

    [ObservableProperty]
    public partial WorkspaceChangeModel? SelectedChange { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync()
    {
        GenerateChangeList();

        return base.OnInitializeAsync();
    }

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
                changes.Add(
                    new()
                    {
                        RelativePath = liveEntry,
                        Name = Path.GetFileName(livePath),
                        Kind = kind,
                    }
                );
            }
        }

        Changes.AddRange(changes);
    }

    #endregion

    private IReadOnlyList<string> ScanFolder(string folder)
    {
        var root = new DirectoryInfo(folder);
        if (!root.Exists)
        {
            return [];
        }

        return
        [
            .. root.EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(folder, file.FullName)),
        ];
    }

    private WorkspaceChangeKind Diff(string live, string import)
    {
        // 这里使用 mtime 而不是哈希，live 的文件是从 import 复制的，atime, ctime, mtime 都是相同的
        if (File.Exists(import))
        {
            var liveTime = File.GetLastWriteTimeUtc(live);
            var importTime = File.GetLastWriteTimeUtc(import);

            if (liveTime > importTime)
            {
                return WorkspaceChangeKind.Updated;
            }
            else if (liveTime < importTime)
            {
                return WorkspaceChangeKind.Outdated;
            }
            else
            {
                return WorkspaceChangeKind.Same;
            }
        }

        return WorkspaceChangeKind.Deleted;
    }
}
