using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Core.Services;

namespace Polymerium.App.PageModels;

public partial class InstanceWorkspacePageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceManager instanceManager,
    NotificationService notificationService,
    OverlayService overlayService,
    ProfileManager profileManager) : InstancePageModelBase(context, instanceManager, profileManager)
{
    #region Direct

    public bool IsLocked => Basic.Source is not null;

    #endregion

    #region Reactive

    public ObservableCollection<WorkspaceChangeModel> Changes { get; } = [];

    [ObservableProperty]
    public partial WorkspaceChangeModel? SelectedChange { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        GenerateChangeList();

        return base.OnInitializeAsync(token);
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
                var file = new FileInfo(livePath);
                var type = Path.GetExtension(livePath).TrimStart('.');
                changes.Add(new()
                {
                    RelativePath = liveEntry,
                    FileName = Path.GetFileName(livePath),
                    Kind = kind,
                    LivePath = livePath,
                    ImportPath = importPath,
                    FileType = type,
                    FileSizeRaw = file.Length,
                    FileLastModifiedRaw = file.LastWriteTime,
                });
            }
        }

        Changes.AddRange(changes);
    }

    private IReadOnlyList<string> ScanFolder(string folder)
    {
        var root = new DirectoryInfo(folder);
        if (!root.Exists)
        {
            return [];
        }

        return
        [
            .. root
              .EnumerateFiles("*", SearchOption.AllDirectories)
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

    #endregion

    #region Commands

    private bool CanOpenDiffer(WorkspaceChangeModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanOpenDiffer))]
    private void OpenDiffer(WorkspaceChangeModel? model)
    {
        if (model != null)
        {
            overlayService.PopModal<WorkspaceDiffModal>(model);
        }
    }

    private bool CanStage(WorkspaceChangeModel? model) => !IsLocked && model is not null && File.Exists(model.LivePath);

    [RelayCommand(CanExecute = nameof(CanStage))]
    private async Task Stage(WorkspaceChangeModel? model)
    {
        if (model == null || !File.Exists(model.LivePath))
        {
            return;
        }

        if (!await overlayService.RequestConfirmationAsync("This will overwrite the file from the modpack"))
        {
            return;
        }

        var suc = false;
        if (File.Exists(model.ImportPath))
        {
            // Overwrite
            try
            {
                File.Copy(model.LivePath, model.ImportPath, true);
                suc = true;
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to perform file staging");
            }
        }
        else
        {
            // Is Deleted
            try
            {
                File.Delete(model.LivePath);
                suc = true;
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to perform file staging");
            }
        }

        if (suc)
        {
            model.Kind = WorkspaceChangeKind.Same;
        }
    }

    private bool CanRestore(WorkspaceChangeModel? model) => model is not null && File.Exists(model.ImportPath);

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task Restore(WorkspaceChangeModel? model)
    {
        if (model == null || !File.Exists(model.ImportPath))
        {
            return;
        }

        if (!await overlayService.RequestConfirmationAsync("This will discard the changes from game playing"))
        {
            return;
        }

        var suc = false;
        if (File.Exists(model.LivePath))
        {
            // Restore
            try
            {
                File.Copy(model.ImportPath, model.LivePath, true);
                suc = true;
            }
            catch (Exception ex)
            {
                notificationService.PopMessage(ex, "Failed to perform file staging");
            }
        }

        if (suc)
        {
            model.Kind = WorkspaceChangeKind.Same;
        }
    }

    #endregion
}
