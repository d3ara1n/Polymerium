using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Core.Services;

namespace Polymerium.App.PageModels;

public partial class InstanceWorkspacePageModel : InstancePageModelBase
{
    #region Injected

    private readonly NotificationService _notificationService;
    private readonly OverlayService _overlayService;

    #endregion

    /// <inheritdoc/>
    public InstanceWorkspacePageModel(
        IViewContext<InstanceContextParameter> context,
        InstanceManager instanceManager,
        NotificationService notificationService,
        OverlayService overlayService,
        ProfileManager profileManager) : base(context, instanceManager, profileManager)
    {
        _notificationService = notificationService;
        _overlayService = overlayService;

        var filter = this.WhenValueChanged(x => x.FilterText).Select(BuildFilter);
        _changesSource.Connect().Filter(filter).Bind(out var view).Subscribe().DisposeWith(_subscriptions);
        ChangesView = view;
    }

    #region Fields

    private readonly CompositeDisposable _subscriptions = new();
    private CancellationToken? _initToken;
    private readonly SourceCache<WorkspaceChangeModel, string> _changesSource = new(x => x.RelativePath);

    #endregion

    #region Direct

    public bool IsLocked => Basic.Source is not null;

    #endregion

    #region Reactive

    public ReadOnlyObservableCollection<WorkspaceChangeModel> ChangesView { get; }

    [ObservableProperty]
    public partial WorkspaceChangeModel? SelectedChange { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; }

    [ObservableProperty]
    public partial int ChangesCount { get; set; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        _initToken = token;
        await GenerateChangeListAsync(token);
    }

    protected override Task OnDeinitializeAsync()
    {
        _subscriptions.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    #region Other

    private static Func<WorkspaceChangeModel, bool> BuildFilter(string? text) =>
        x => string.IsNullOrEmpty(text) || x.RelativePath.Contains(text, StringComparison.CurrentCultureIgnoreCase);

    private async Task GenerateChangeListAsync(CancellationToken token)
    {
        // live / import 的变更
        // 由于 import -> live 是全量文件复制，live 的文件只会比 import 多（但存在 .keep 映射了目录时，游戏会往目录里塞东西）
        // 当 live 比 import 多时需要同步，但当 live 比 import 少（比如新增的没复制过来）就不需要在乎

        var liveDir = PathDef.Default.DirectoryOfLive(Basic.Key);
        var importDir = PathDef.Default.DirectoryOfImport(Basic.Key);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _changesSource.Clear();
            ChangesCount = _changesSource.Count;
        });

        var batch = new List<WorkspaceChangeModel>(200);
        foreach (var liveEntry in ScanFolder(liveDir, token))
        {
            var livePath = Path.Combine(liveDir, liveEntry);
            var importPath = Path.Combine(importDir, liveEntry);
            var kind = Diff(livePath, importPath);
            if (kind != WorkspaceChangeKind.Same)
            {
                var file = new FileInfo(livePath);
                var type = Path.GetExtension(livePath).TrimStart('.');
                batch.Add(new()
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
                if (batch.Count >= 100)
                {
                    var toAdd = batch.ToArray();
                    batch.Clear();
                    await Dispatcher.UIThread.InvokeAsync(() => _changesSource.AddOrUpdate(toAdd));
                }
            }
        }

        if (batch.Count > 0)
        {
            await Dispatcher.UIThread.InvokeAsync(() => _changesSource.AddOrUpdate(batch));
        }

        await Dispatcher.UIThread.InvokeAsync(() => ChangesCount = _changesSource.Count);
    }

    private IEnumerable<string> ScanFolder(string folder, CancellationToken token)
    {
        var root = new DirectoryInfo(folder);
        if (!root.Exists)
        {
            yield break;
        }

        foreach (var file in root.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            if (token.IsCancellationRequested)
                yield break;
            yield return Path.GetRelativePath(folder, file.FullName);
        }
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

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (_initToken is not null && _initToken.Value.IsCancellationRequested)
        {
            return;
        }

        await GenerateChangeListAsync(_initToken ?? CancellationToken.None);
    }

    private bool CanOpenDiffer(WorkspaceChangeModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanOpenDiffer))]
    private async Task OpenDiffer(WorkspaceChangeModel? model)
    {
        if (model != null)
        {
            if (model.FileSizeRaw > 1024 * 1024 * 1024
             && !await _overlayService
                    .RequestConfirmationAsync("File is too large. It may take time to compute and render diff."))
            {
                // 文件大且用户拒绝
                return;
            }

            _overlayService.PopModal<WorkspaceDiffModal>(model);
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

        if (!await _overlayService.RequestConfirmationAsync("This will overwrite the file from the modpack"))
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
                _notificationService.PopMessage(ex, "Failed to perform file staging");
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
                _notificationService.PopMessage(ex, "Failed to perform file staging");
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

        if (!await _overlayService.RequestConfirmationAsync("This will discard the changes from game playing"))
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
                _notificationService.PopMessage(ex, "Failed to perform file staging");
            }
        }

        if (suc)
        {
            model.Kind = WorkspaceChangeKind.Same;
        }
    }

    #endregion
}
