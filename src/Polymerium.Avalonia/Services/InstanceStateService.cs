using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Lifetimes;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Engines.Launching;
using TridentCore.Core.Engines.Deploying;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Services;

// 实例派生状态的唯一入口。机制只负责「单飞 + 驻留 + 判据 + 失效」，每个模块的状态与取数逻辑就地写在
// 自己的 RetrieveXxxAsync 里——新增一个状态 = 一个 XxxState + 一个 RetrieveXxxAsync。状态只描述事实，
// 消费方转成自己的 Model 再交给视图。
public class InstanceStateService(
    ILogger<InstanceStateService> logger,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    PersistenceService persistenceService,
    DeploymentPlanner deploymentPlanner,
    DeploymentDiffer deploymentDiffer,
    SourceProjectionPlanner sourceProjectionPlanner,
    ProjectionArbitrator projectionArbitrator) : ILifetimeService
{
    #region Nested type: StateBase

    // 时间戳由机制写：命中驻留时它保持当初判定的时刻，而不是「现在」。
    public abstract class StateBase
    {
        public DateTimeOffset CapturedAt { get; internal set; }
    }

    #endregion

    #region Nested type: GitState

    public sealed class GitState : StateBase
    {
        public bool IsRepository { get; init; }
        public string BranchName { get; init; } = string.Empty;
        public string HeadSummary { get; init; } = string.Empty;
        public string TrackingBranchName { get; init; } = string.Empty;
        public bool IsHeadDetached { get; init; }
        public int StagedCount { get; init; }
        public int UnstagedCount { get; init; }
        public int ChangedCount { get; init; }
        public int AheadCount { get; init; }
        public int BehindCount { get; init; }
    }

    #endregion

    #region Nested type: ChangeState

    // Pack Source 与运行目录之间的工作副本差异。条目携带两侧真实路径与工作副本元数据，
    // 消费方（Workspace 审阅列表）直接映射，不再自行扫描。
    public sealed class ChangeState : StateBase
    {
        public IReadOnlyList<Change> Entries { get; init; } = [];

        public sealed class Change
        {
            public required string RelativePath { get; init; }
            public required string FileName { get; init; }
            public required string LivePath { get; init; }
            public required string ImportPath { get; init; }
            public required string FileType { get; init; }
            public required long FileSize { get; init; }
            public required DateTime FileLastModified { get; init; }
            public required WorkspaceChangeKind Kind { get; init; }
        }
    }

    #endregion

    #region Nested type: PackSourceState

    public sealed class PackSourceState : StateBase
    {
        public int FileCount { get; init; }
    }

    #endregion

    #region Nested type: PatchState

    public sealed class PatchState : StateBase
    {
        public int ImportCount { get; init; }
        public int UserCount { get; init; }
    }

    #endregion

    #region Nested type: AttachmentState

    // 附件是约定文件名，不存在就是 null；发布准备是否齐全由视图判定。
    public sealed class AttachmentState : StateBase
    {
        public string? IconPath { get; init; }
        public string? ReadmePath { get; init; }
        public string? ChangelogPath { get; init; }
        public string? LicensePath { get; init; }
    }

    #endregion

    #region Nested type: LastRunState

    public sealed class LastRunState : StateBase
    {
        public DateTimeOffset? EndedAt { get; init; }
        public LaunchOutcome? Outcome { get; init; }
        public int CrashCount { get; init; }
    }

    #endregion

    public enum DeploymentReadiness { NeedsDeployment, NeedsDownload, Ready }

    public enum DownloadCategory { Library, Package, Asset, Runtime, Other }

    public sealed record DownloadCheck(DownloadCategory Category, bool IsReady);

    public sealed class DeploymentState : StateBase
    {
        public DeploymentReadiness Readiness { get; init; }
        public IReadOnlyList<DownloadCheck>? DownloadChecks { get; init; }
    }

    public Task<DeploymentState> RetrieveDeploymentStateAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key,
            ct => Task.Run(async () =>
            {
                if (instanceManager.IsInUse(key)) return false;
                var data = await LockValidationHelper.ReadAsync(key, ct).ConfigureAwait(false);
                return data is not null && await LockValidationHelper.ValidateAsync(key, profileManager.GetImmutable(key).Setup, data, ct).ConfigureAwait(false);
            }, ct),
            ct => Task.Run(async () =>
            {
                try
                {
                    if (instanceManager.IsInUse(key)) return new();
                    var data = await LockValidationHelper.ReadAsync(key, ct).ConfigureAwait(false);
                    if (data is null || !await LockValidationHelper.ValidateAsync(key, profileManager.GetImmutable(key).Setup, data, ct).ConfigureAwait(false))
                        return new();
                    var target = deploymentPlanner.CreateTarget(key, data, ct);
                    var missing = new List<DownloadCategory>();
                    var assets = await DeploymentIndexHelper.ReadAssetAsync(data.Artifact!.AssetIndex, ct).ConfigureAwait(false);
                    if (assets is null) missing.Add(DownloadCategory.Asset);
                    else new AssetPlanner().Plan(target, assets, ct);
                    if (data.RuntimeMajor is { } major)
                    {
                        var runtime = await DeploymentIndexHelper.ReadRuntimeAsync(major, data.RuntimeIndex?.Hash, ct).ConfigureAwait(false);
                        if (runtime is null) missing.Add(DownloadCategory.Runtime);
                        else new RuntimePlanner().Plan(target, runtime, ct);
                    }
                    var plan = deploymentDiffer.Diff(key, target, ct);
                    var libraryRoot = PathDef.Default.CacheLibraryDirectory;
                    var packageRoot = PathDef.Default.CachePackageDirectory;
                    var assetRoot = PathDef.Default.CacheAssetDirectory;
                    var runtimeRoot = PathDef.Default.CacheRuntimeDirectory;
                    if (plan.Downloads.Any(x => FileHelper.IsInDirectory(x.Path, libraryRoot))) missing.Add(DownloadCategory.Library);
                    if (plan.Downloads.Any(x => FileHelper.IsInDirectory(x.Path, packageRoot))) missing.Add(DownloadCategory.Package);
                    if (plan.Downloads.Any(x => FileHelper.IsInDirectory(x.Path, assetRoot))) missing.Add(DownloadCategory.Asset);
                    if (plan.Downloads.Any(x => FileHelper.IsInDirectory(x.Path, runtimeRoot))) missing.Add(DownloadCategory.Runtime);
                    if (plan.Downloads.Any(x => !FileHelper.IsInDirectory(x.Path, libraryRoot)
                        && !FileHelper.IsInDirectory(x.Path, packageRoot)
                        && !FileHelper.IsInDirectory(x.Path, assetRoot)
                        && !FileHelper.IsInDirectory(x.Path, runtimeRoot))) missing.Add(DownloadCategory.Other);
                    var needsLocalWork = plan.Operations.Count != 0 || plan.NeedsManifestCommit;
                    return new()
                    {
                        Readiness = missing.Count != 0 ? DeploymentReadiness.NeedsDownload
                            : needsLocalWork ? DeploymentReadiness.NeedsDeployment : DeploymentReadiness.Ready,
                        DownloadChecks = [.. Enum.GetValues<DownloadCategory>()
                            .Where(x => x != DownloadCategory.Other || missing.Contains(x))
                            .Select(x => new DownloadCheck(x, !missing.Contains(x)))]
                    };
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Deployment state probe failed for {key}", key);
                    return new DeploymentState();
                }
            }, ct), token);

    #region Mechanism

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, Slot>> _slots = new();
    private readonly Dictionary<string, Guid> _activeActivities = [];
    private readonly List<IDisposable> _subscriptions = [];
    private readonly Subject<string> _changed = new();
    private readonly Lock _sync = new();
    private CancellationTokenSource? _lifetime;

    // 单个 (实例, 状态类型) 的驻留槽：字段只在 _gate 内变更，锁纪律收在类内，RetrieveAsync 只做编排。
    private sealed class Slot
    {
        private readonly object _gate = new();

        private StateBase? _retained;
        private Task? _inFlight;
        private long _inFlightGeneration;
        private long _generation;

        public StateBase? PeekRetained()
        {
            lock (_gate)
            {
                return _retained;
            }
        }

        public void Invalidate()
        {
            lock (_gate)
            {
                _generation++;
                _retained = null;
            }
        }

        // NOTE: 只放行同代际且仍在途的计算。已完成的任务视同无在途——轻状态每次都重算；
        //  旧代际任务是失效前发起的，后来者并入只会拿到已被撤销的数据。
        public Task<TState>? JoinInFlight<TState>() where TState : StateBase
        {
            lock (_gate)
            {
                return _inFlight is Task<TState> task && !task.IsCompleted && _inFlightGeneration == _generation
                    ? task
                    : null;
            }
        }

        public Task<TState> BeginCompute<TState>(Func<CancellationToken, Task<TState>> compute,
                                                 CancellationToken lifetime)
            where TState : StateBase
        {
            lock (_gate)
            {
                var generation = _generation;
                var task = RunAsync(this, generation, compute, lifetime);
                _inFlight = task;
                _inFlightGeneration = generation;
                return task;
            }
        }

        public void Retain(long generation, StateBase value)
        {
            lock (_gate)
            {
                // NOTE: 代际不匹配说明计算期间状态已被撤销（部署开始、profile 变更等），结果不得进驻留。
                if (_generation == generation)
                {
                    _retained = value;
                }
            }
        }

        // NOTE: 失效后新计算会顶替登记，旧任务的收尾不得把新任务的在途标记清掉。
        public void ReleaseInFlight(long generation)
        {
            lock (_gate)
            {
                if (_inFlightGeneration == generation)
                {
                    _inFlight = null;
                }
            }
        }

        private static async Task<TState> RunAsync<TState>(Slot slot,
                                                          long generation,
                                                          Func<CancellationToken, Task<TState>> compute,
                                                          CancellationToken lifetime)
            where TState : StateBase
        {
            try
            {
                var value = await compute(lifetime).ConfigureAwait(false);
                value.CapturedAt = DateTimeOffset.Now;
                slot.Retain(generation, value);
                return value;
            }
            finally
            {
                slot.ReleaseInFlight(generation);
            }
        }
    }

    /// <summary>实例状态被撤销的通知（含部署开始、profile 更新、实例移除）。</summary>
    public IObservable<string> Changed => _changed;

    private Task<TState> RetrieveAsync<TState>(string key,
                                               Func<CancellationToken, Task<TState>> compute,
                                               CancellationToken token)
        where TState : StateBase => RetrieveAsync(key, null, compute, token);

    /// <summary>取状态。有判据时先问判据，有效直接复用驻留值；否则重算。</summary>
    /// <param name="stillValid">
    ///     便宜判据（如部署锁的配置指纹比对）。为 null 表示状态没有可靠判据，每次都重算；判据抛异常按失效处理。
    ///     资源就绪状态以锁有效性复用本会话的检查结果，不监控外部文件修改。
    /// </param>
    private async Task<TState> RetrieveAsync<TState>(string key,
                                                     Func<CancellationToken, Task<bool>>? stillValid,
                                                     Func<CancellationToken, Task<TState>> compute,
                                                     CancellationToken token)
        where TState : StateBase
    {
        var slot = SlotOf(key, typeof(TState));

        if (stillValid is not null && slot.PeekRetained() is TState cached)
        {
            bool valid;
            try
            {
                valid = await stillValid(_lifetime?.Token ?? CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "State validity probe failed for {key}/{state}", key, typeof(TState).Name);
                valid = false;
            }

            if (valid)
            {
                return cached;
            }
        }

        var pending = slot.JoinInFlight<TState>()
                     ?? slot.BeginCompute(compute, _lifetime?.Token ?? CancellationToken.None);

        // 计算不随调用方取消：结果要进驻留，取消只结束本次等待。
        return await pending.WaitAsync(token).ConfigureAwait(false);
    }

    private Slot SlotOf(string key, Type state) =>
        _slots.GetOrAdd(key, _ => new()).GetOrAdd(state, _ => new());

    /// <summary>撤销某实例的全部驻留状态并通知消费方。修实例产物的操作在开始时就该调用。</summary>
    public void Invalidate(string key)
    {
        if (_slots.TryGetValue(key, out var slots))
        {
            foreach (var slot in slots.Values)
            {
                slot.Invalidate();
            }
        }

        Dispatcher.UIThread.Post(() => _changed.OnNext(key));
    }

    #endregion

    #region Git

    public Task<GitState> RetrieveGitAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key, ct => Task.Run(() => ProbeGit(key), ct), token);

    private GitState ProbeGit(string key)
    {
        var home = PathDef.Default.DirectoryOfHome(key);

        try
        {
            var discovered = Repository.Discover(home);
            if (string.IsNullOrEmpty(discovered))
            {
                return new() { IsRepository = false };
            }

            using var repository = new Repository(discovered);
            if (!FileHelper.IsPathEquivalent(repository.Info.WorkingDirectory, home))
            {
                return new() { IsRepository = false };
            }

            var isDetached = repository.Info.IsHeadDetached;
            var trackingDetails = repository.Head.TrackingDetails;
            var status =
                repository.RetrieveStatus(new StatusOptions { IncludeIgnored = false, RecurseUntrackedDirs = true });

            var staged = 0;
            var unstaged = 0;
            var changed = 0;
            foreach (var entry in status)
            {
                if (GitStatusHelper.IsStaged(entry.State))
                {
                    staged++;
                }

                if (GitStatusHelper.IsUnstaged(entry.State))
                {
                    unstaged++;
                }

                changed++;
            }

            return new()
            {
                IsRepository = true,
                BranchName = isDetached ? "Detached HEAD" : repository.Head.FriendlyName,
                HeadSummary = BuildHeadSummary(repository),
                TrackingBranchName = repository.Head.TrackedBranch?.FriendlyName ?? "No upstream",
                IsHeadDetached = isDetached,
                StagedCount = staged,
                UnstagedCount = unstaged,
                ChangedCount = changed,
                AheadCount = trackingDetails.AheadBy ?? 0,
                BehindCount = trackingDetails.BehindBy ?? 0
            };
        }
        catch (Exception ex)
        {
            // NOTE: 状态检测不得把首页拖塌；读不出的仓库按「未启用版本控制」上报，具体错误留给
            //  Workspace 的实际操作给出。
            logger.LogWarning(ex, "Git status probe failed for {key}", key);
            return new() { IsRepository = false };
        }
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

    #endregion

    #region Changes

    public Task<ChangeState> RetrieveChangesAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key, ct => Task.Run(() => ProbeChanges(key, ct), ct), token);

    private ChangeState ProbeChanges(string key, CancellationToken token)
    {
        var buildDirectory = PathDef.Default.DirectoryOfBuild(key);
        var importDirectory = PathDef.Default.DirectoryOfImport(key);
        var entries = new List<ChangeState.Change>();
        var sourceProjections = sourceProjectionPlanner.CreateTarget(key, token);
        var relativePaths = sourceProjections
            .Where(x => x.Kind == DeploymentTarget.ProjectionKind.Import)
            .Select(x => ProjectionManifestHelper.ToStoredPath(buildDirectory, x.Target))
            .ToHashSet(FileHelper.PathComparer);
        if (ProjectionManifestHelper.HasImportManifest(key))
        {
            foreach (var relative in ProjectionManifestHelper.ReadImport(key).Files)
            {
                var livePath = ProjectionManifestHelper.ResolveStoredPath(buildDirectory, relative);
                var importPath = DeploymentFileHelper.ProjectionPath(importDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
                var candidate = new DeploymentTarget.Projection(
                    importPath, livePath, DeploymentTarget.ProjectionKind.Import, false, null, null);
                if (!projectionArbitrator.IsShadowed(candidate, sourceProjections)) relativePaths.Add(relative);
            }
        }
        foreach (var relative in relativePaths.OrderBy(x => x, FileHelper.PathComparer))
        {
            if (token.IsCancellationRequested) break;
            var livePath = ProjectionManifestHelper.ResolveStoredPath(buildDirectory, relative);
            var importPath = DeploymentFileHelper.ProjectionPath(importDirectory, relative.Replace('/', Path.DirectorySeparatorChar));

            // 符号链接是部署投影，不是工作副本，不参与比对。
            if (!File.Exists(livePath) || DeploymentFileHelper.HasLinkAtOrAbove(livePath, buildDirectory)) continue;

            var kind = Diff(livePath, importPath);
            if (kind == WorkspaceChangeKind.Same) continue;

            var live = new FileInfo(livePath);
            entries.Add(new()
            {
                RelativePath = relative.Replace('/', Path.DirectorySeparatorChar),
                FileName = live.Name,
                LivePath = livePath,
                ImportPath = importPath,
                FileType = Path.GetExtension(livePath).TrimStart('.'),
                FileSize = live.Length,
                FileLastModified = live.LastWriteTime,
                Kind = kind
            });
        }

        return new() { Entries = entries };
    }

    private static WorkspaceChangeKind Diff(string live, string import)
    {
        // 用 mtime 而非哈希——工作副本由 import 复制到 build，atime/ctime/mtime 全相同。
        if (!File.Exists(import))
        {
            return WorkspaceChangeKind.Deleted;
        }

        var liveTime = File.GetLastWriteTimeUtc(live);
        var importTime = File.GetLastWriteTimeUtc(import);

        if (liveTime > importTime)
        {
            return WorkspaceChangeKind.Updated;
        }

        return liveTime < importTime ? WorkspaceChangeKind.Outdated : WorkspaceChangeKind.Same;
    }

    #endregion

    #region Pack source

    public Task<PackSourceState> RetrievePackSourceAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key, ct => Task.Run(() => ProbePackSource(key, ct), ct), token);

    private static PackSourceState ProbePackSource(string key, CancellationToken token)
    {
        var root = new DirectoryInfo(PathDef.Default.DirectoryOfImport(key));
        var count = 0;

        if (root.Exists)
        {
            foreach (var _ in root.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                count++;
            }
        }

        return new() { FileCount = count };
    }

    #endregion

    #region Patches

    public Task<PatchState> RetrievePatchesAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key,
                      async ct =>
                      {
                          var index = await PatchStorageHelper
                                            .ReadIndexAtAsync(PathDef.Default.DirectoryOfPatches(key), ct)
                                            .ConfigureAwait(false);
                          return new PatchState { ImportCount = index.Import.Count, UserCount = index.Users.Count };
                      },
                      token);

    #endregion

    #region Attachments

    public Task<AttachmentState> RetrieveAttachmentsAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key, ct => Task.Run(() => ProbeAttachments(key), ct), token);

    private static AttachmentState ProbeAttachments(string key)
    {
        var home = PathDef.Default.DirectoryOfHome(key);
        return new()
        {
            IconPath = InstanceHelper.PickIcon(key),
            ReadmePath = Probe(home, "README.md"),
            ChangelogPath = Probe(home, "CHANGELOG.md"),
            LicensePath = Probe(home, "LICENSE.txt")
        };

        static string? Probe(string directory, string name)
        {
            var path = Path.Combine(directory, name);
            return File.Exists(path) ? path : null;
        }
    }

    #endregion

    #region Last run

    public Task<LastRunState> RetrieveLastRunAsync(string key, CancellationToken token = default) =>
        RetrieveAsync(key, ct => Task.Run(() => ProbeLastRun(key), ct), token);

    private LastRunState ProbeLastRun(string key)
    {
        var activity = persistenceService.GetLastActivity(key);
        return new()
        {
            EndedAt = DateTimeHelper.FromPersistedLocalDateTime(activity?.End),
            Outcome = activity?.Outcome,
            CrashCount = persistenceService.GetCrashCount(key)
        };
    }

    #endregion

    #region Lifetimes

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _subscriptions.Add(instanceManager.Activities.Subscribe(OnActivity));
        profileManager.ProfileUpdated += OnProfileUpdated;
        profileManager.ProfileRemoved += OnProfileRemoved;
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        profileManager.ProfileUpdated -= OnProfileUpdated;
        profileManager.ProfileRemoved -= OnProfileRemoved;
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        return ValueTask.CompletedTask;
    }

    // NOTE: 一次活动会持续发布进度帧，只有「新活动开始」与「落终态」才是状态真变了——
    //  逐帧失效会在部署期间每秒撤销几十次驻留。
    private void OnActivity(InstanceActivity activity)
    {
        bool invalidate;
        lock (_sync)
        {
            var known = _activeActivities.TryGetValue(activity.Key, out var id);
            if (activity.IsCompleted)
            {
                _activeActivities.Remove(activity.Key);
                invalidate = true;
            }
            else if (!known || id != activity.Id)
            {
                _activeActivities[activity.Key] = activity.Id;
                invalidate = true;
            }
            else
            {
                invalidate = false;
            }
        }

        if (invalidate)
        {
            Invalidate(activity.Key);
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e) => Invalidate(e.Key);

    private void OnProfileRemoved(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        _slots.TryRemove(e.Key, out _);
        Invalidate(e.Key);
    }

    #endregion
}
