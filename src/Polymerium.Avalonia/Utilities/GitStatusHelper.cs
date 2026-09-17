using LibGit2Sharp;

namespace Polymerium.Avalonia.Utilities;

// 索引区与工作区的改动判定：同一份文件状态既供实例状态检测，也供 Workspace 的还原操作使用。
public static class GitStatusHelper
{
    public static bool IsStaged(FileStatus status) =>
        status.HasFlag(FileStatus.NewInIndex)
     || status.HasFlag(FileStatus.ModifiedInIndex)
     || status.HasFlag(FileStatus.DeletedFromIndex)
     || status.HasFlag(FileStatus.RenamedInIndex)
     || status.HasFlag(FileStatus.TypeChangeInIndex);

    public static bool IsUnstaged(FileStatus status) =>
        status.HasFlag(FileStatus.NewInWorkdir)
     || status.HasFlag(FileStatus.ModifiedInWorkdir)
     || status.HasFlag(FileStatus.DeletedFromWorkdir)
     || status.HasFlag(FileStatus.RenamedInWorkdir)
     || status.HasFlag(FileStatus.TypeChangeInWorkdir);
}
