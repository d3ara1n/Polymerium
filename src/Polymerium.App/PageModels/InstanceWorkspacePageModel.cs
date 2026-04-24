using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using LibGit2Sharp;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Core.Services;
using Trident.Core.Utilities;

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

    [ObservableProperty]
    public partial bool IsGitRepository { get; set; }

    [ObservableProperty]
    public partial string GitBranchName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GitHeadSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int GitStagedCount { get; set; }

    [ObservableProperty]
    public partial int GitUnstagedCount { get; set; }

    [ObservableProperty]
    public partial int GitChangedCount { get; set; }

    [ObservableProperty]
    public partial int GitAheadCount { get; set; }

    [ObservableProperty]
    public partial int GitBehindCount { get; set; }

    [ObservableProperty]
    public partial string GitTrackingBranchName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool GitIsDetachedHead { get; set; }

    [ObservableProperty]
    public partial bool GitIsBusy { get; set; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        _initToken = token;

        await LoadGitStatusAsync();
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

    private async Task LoadGitStatusAsync()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        var discoveredPath = Repository.Discover(dir);
        if (string.IsNullOrEmpty(discoveredPath))
        {
            await ResetGitStatusAsync();
            return;
        }

        using var repository = new Repository(discoveredPath);
        if (!FileHelper.IsPathEquivalent(repository.Info.WorkingDirectory, dir))
        {
            await ResetGitStatusAsync();
            return;
        }

        var isDetached = repository.Info.IsHeadDetached;
        var trackedBranch = repository.Head.TrackedBranch;
        var trackingDetails = repository.Head.TrackingDetails;
        var branchName = isDetached ? "Detached HEAD" : repository.Head.FriendlyName;
        var headSummary = BuildHeadSummary(repository);
        var status =
            repository.RetrieveStatus(new StatusOptions { IncludeIgnored = false, RecurseUntrackedDirs = true });

        var stagedCount = 0;
        var unstagedCount = 0;
        var changedCount = 0;
        foreach (var entry in status)
        {
            if (IsStaged(entry.State))
            {
                stagedCount++;
            }

            if (IsUnstaged(entry.State))
            {
                unstagedCount++;
            }

            changedCount++;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsGitRepository = true;
            GitBranchName = branchName;
            GitHeadSummary = headSummary;
            GitStagedCount = stagedCount;
            GitUnstagedCount = unstagedCount;
            GitChangedCount = changedCount;
            GitAheadCount = trackingDetails.AheadBy ?? 0;
            GitBehindCount = trackingDetails.BehindBy ?? 0;
            GitTrackingBranchName = trackedBranch?.FriendlyName ?? "No upstream";
            GitIsDetachedHead = isDetached;
        });

        RefreshGitCommands();
    }

    private async Task ResetGitStatusAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsGitRepository = false;
            GitBranchName = string.Empty;
            GitHeadSummary = string.Empty;
            GitStagedCount = 0;
            GitUnstagedCount = 0;
            GitChangedCount = 0;
            GitAheadCount = 0;
            GitBehindCount = 0;
            GitTrackingBranchName = string.Empty;
            GitIsDetachedHead = false;
        });

        RefreshGitCommands();
    }

    private static string BuildHeadSummary(Repository repository)
    {
        var tip = repository.Head.Tip;
        if (tip is null)
        {
            return "No commits yet";
        }

        var shortSha = tip.Sha[..7];
        var tag = repository.Tags.FirstOrDefault(tag => tag.PeeledTarget is Commit commit && commit.Sha == tip.Sha)
                           ?.FriendlyName;

        return string.IsNullOrEmpty(tag) ? shortSha : $"{shortSha} ({tag})";
    }

    private static bool IsStaged(FileStatus status) =>
        status.HasFlag(FileStatus.NewInIndex)
     || status.HasFlag(FileStatus.ModifiedInIndex)
     || status.HasFlag(FileStatus.DeletedFromIndex)
     || status.HasFlag(FileStatus.RenamedInIndex)
     || status.HasFlag(FileStatus.TypeChangeInIndex);

    private static bool IsUnstaged(FileStatus status) =>
        status.HasFlag(FileStatus.NewInWorkdir)
     || status.HasFlag(FileStatus.ModifiedInWorkdir)
     || status.HasFlag(FileStatus.DeletedFromWorkdir)
     || status.HasFlag(FileStatus.RenamedInWorkdir)
     || status.HasFlag(FileStatus.TypeChangeInWorkdir);

    private bool TryOpenGitRepository(out Repository? repository)
    {
        repository = null;

        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        var discoveredPath = Repository.Discover(dir);
        if (string.IsNullOrEmpty(discoveredPath))
        {
            return false;
        }

        var candidate = new Repository(discoveredPath);
        if (!FileHelper.IsPathEquivalent(candidate.Info.WorkingDirectory, dir))
        {
            candidate.Dispose();
            return false;
        }

        repository = candidate;
        return true;
    }

    private void RefreshGitCommands()
    {
        CommitGitCommand.NotifyCanExecuteChanged();
        RestoreGitChangesCommand.NotifyCanExecuteChanged();
    }

    private void SetGitBusy(bool value)
    {
        GitIsBusy = value;
        RefreshGitCommands();
    }

    private async Task RunGitOperationAsync(string errorTitle, Func<Repository, Task> operation)
    {
        if (!TryOpenGitRepository(out var repository))
        {
            await ResetGitStatusAsync();
            _notificationService.PopMessage("The directory is not in a git repository.", "Git error", GrowlLevel.Warning);
            return;
        }

        SetGitBusy(true);
        try
        {
            using var openedRepository = repository
                ?? throw new InvalidOperationException("Git repository was not opened.");
            await operation(openedRepository);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, errorTitle);
        }
        finally
        {
            SetGitBusy(false);
            await LoadGitStatusAsync();
        }
    }

    private static void DeleteWorkingTreeEntry(string repositoryRoot, string relativePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refused to delete path outside repository root.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            TrimEmptyParents(root, Path.GetDirectoryName(fullPath));
            return;
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }

    private static void TrimEmptyParents(string repositoryRoot, string? current)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));
        while (!string.IsNullOrEmpty(current) && !FileHelper.IsPathEquivalent(current, root))
        {
            if (Directory.EnumerateFileSystemEntries(current).Any())
            {
                break;
            }

            Directory.Delete(current);
            current = Path.GetDirectoryName(current);
        }
    }

    private static string BuildRestoreConfirmationMessage(int unstagedCount)
    {
        var builder = new StringBuilder();
        builder.AppendLine("This will restore unstaged changes in the current working tree.\n");
        builder.AppendLine($"Affected entries: {unstagedCount}");
        builder.AppendLine("- Tracked files will be restored to HEAD");
        builder.AppendLine("- Untracked files and directories will be deleted");
        builder.AppendLine("- Commit history and remotes will not be modified");
        return builder.ToString();
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

        await LoadGitStatusAsync();
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
            await LoadGitStatusAsync();
        }
    }

    private bool CanRestore(WorkspaceChangeModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task Restore(WorkspaceChangeModel? model)
    {
        if (model == null)
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
                if (File.Exists(model.ImportPath))
                {
                    File.Copy(model.ImportPath, model.LivePath, true);
                }
                else
                {
                    File.Delete(model.LivePath);
                }

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
            await LoadGitStatusAsync();
        }
    }

    private bool CanCommitGit() =>
        IsGitRepository && !GitIsBusy && !GitIsDetachedHead && GitChangedCount > 0;

    [RelayCommand(CanExecute = nameof(CanCommitGit))]
    private async Task CommitGit()
    {
        await RunGitOperationAsync("Git commit failed", async repository =>
        {
            var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
            if (signature is null)
            {
                _notificationService.PopMessage(
                    "No Git identity is configured for this repository. Configure user.name and user.email first.",
                    "Git commit failed",
                    GrowlLevel.Warning
                );
                return;
            }

            var message = await _overlayService.RequestInputAsync(
                "Enter a commit message. All current changes will be staged before commit.",
                "Git commit",
                $"Update {Basic.Name}"
            );
            if (message is null)
            {
                return;
            }

            var trimmedMessage = message.Trim();
            if (string.IsNullOrEmpty(trimmedMessage))
            {
                _notificationService.PopMessage("Commit message cannot be empty.", "Git commit failed", GrowlLevel.Warning);
                return;
            }

            var paths = repository
                .RetrieveStatus(new StatusOptions { IncludeIgnored = false, RecurseUntrackedDirs = true })
                .Select(entry => entry.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (paths.Length == 0)
            {
                _notificationService.PopMessage("No changes to commit.", "Git commit", GrowlLevel.Information);
                return;
            }

            Commands.Stage(repository, paths);

            var commit = repository.Commit(trimmedMessage, signature, signature);
            _notificationService.PopMessage(
                $"Created commit {commit.Sha[..7]}",
                "Git commit succeeded",
                GrowlLevel.Success
            );
        });
    }

    private bool CanRestoreGitChanges() =>
        IsGitRepository && !GitIsBusy && GitStagedCount == 0 && GitUnstagedCount > 0;

    [RelayCommand(CanExecute = nameof(CanRestoreGitChanges))]
    private async Task RestoreGitChanges()
    {
        if (!await _overlayService.RequestConfirmationAsync(
                BuildRestoreConfirmationMessage(GitUnstagedCount),
                "Restore Git working tree"
            ))
        {
            return;
        }

        await RunGitOperationAsync("Git restore failed", repository =>
        {
            var status = repository.RetrieveStatus(
                new StatusOptions { IncludeIgnored = false, RecurseUntrackedDirs = true }
            );

            var trackedPaths = status
                .Where(entry => IsUnstaged(entry.State) && !entry.State.HasFlag(FileStatus.NewInWorkdir))
                .Select(entry => entry.FilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (trackedPaths.Length > 0)
            {
                if (repository.Head.Tip is null)
                {
                    throw new InvalidOperationException("Cannot restore tracked files because HEAD does not exist.");
                }

                foreach (var path in trackedPaths)
                {
                    Commands.Checkout(
                        repository,
                        repository.Head.Tip.Tree,
                        new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force },
                        path
                    );
                }
            }

            foreach (
                var relativePath in status
                    .Where(entry => entry.State.HasFlag(FileStatus.NewInWorkdir))
                    .Select(entry => entry.FilePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(path => path.Length)
            )
            {
                DeleteWorkingTreeEntry(repository.Info.WorkingDirectory, relativePath);
            }

            _notificationService.PopMessage("Restored unstaged working tree changes.", "Git restore succeeded", GrowlLevel.Success);
            return Task.CompletedTask;
        });
    }

    #endregion
}
