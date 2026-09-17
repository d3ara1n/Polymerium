using System;
using TridentCore.Core.Engines.Launching;

namespace Polymerium.Avalonia.Models;

// 开发总览里的一行状态。每个状态各自成类，只装事实；空与不空是展示策略，由消费方（视图）决定。
public abstract class DevelopmentStatusModel
{
    #region Nested type: Git

    public sealed class Git : DevelopmentStatusModel
    {
        public required bool IsRepository { get; init; }
        public required string BranchName { get; init; }
        public required string HeadSummary { get; init; }
        public required string TrackingBranchName { get; init; }
        public required int AheadCount { get; init; }
        public required int BehindCount { get; init; }
        public required int ChangedCount { get; init; }
    }

    #endregion

    #region Nested type: Unsynced

    public sealed class Unsynced : DevelopmentStatusModel
    {
        public required int Count { get; init; }
    }

    #endregion

    #region Nested type: PackSource

    public sealed class PackSource : DevelopmentStatusModel
    {
        public required int FileCount { get; init; }
    }

    #endregion

    #region Nested type: Patches

    public sealed class Patches : DevelopmentStatusModel
    {
        public required int ImportCount { get; init; }
        public required int UserCount { get; init; }
    }

    #endregion

    #region Nested type: ReleaseFiles

    public sealed class ReleaseFiles : DevelopmentStatusModel
    {
        public required string? IconPath { get; init; }
        public required string? ReadmePath { get; init; }
        public required string? ChangelogPath { get; init; }
        public required string? LicensePath { get; init; }
    }

    #endregion

    #region Nested type: LastRun

    public sealed class LastRun : DevelopmentStatusModel
    {
        public required DateTimeOffset? EndedAt { get; init; }
        public required LaunchOutcome? Outcome { get; init; }
        public required int CrashCount { get; init; }
    }

    #endregion
}
