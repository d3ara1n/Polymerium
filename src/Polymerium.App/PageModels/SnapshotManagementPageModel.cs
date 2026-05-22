using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;

namespace Polymerium.App.PageModels;

public partial class SnapshotManagementPageModel : ViewModelBase
{
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SnapshotItemModel? SelectedSnapshot { get; set; }

    public ObservableCollection<SnapshotItemModel> Snapshots { get; } = [];

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        var items = new[]
        {
            new SnapshotItemModel(
                "Initial Setup",
                "初始安装后的第一次快照",
                "2025-03-15 10:30",
                "1.20.4",
                "Fabric Loader 0.15.6",
                15,
                24,
                67.2
            ),
            new SnapshotItemModel(
                "Before Mod Update",
                "更新 Sodium 和 Lithium 前的备份",
                "2025-03-18 14:22",
                "1.20.4",
                "Fabric Loader 0.15.6",
                28,
                42,
                156.7
            ),
            new SnapshotItemModel(
                "Pre-Cleanup Backup",
                "清理无用模组前的备份",
                "2025-03-20 09:45",
                "1.20.4",
                "Fabric Loader 0.15.6",
                22,
                38,
                134.5
            ),
            new SnapshotItemModel(
                "After Adding Shaders",
                "添加光影包后的状态",
                "2025-03-25 16:10",
                "1.20.4",
                "Fabric Loader 0.15.6",
                31,
                48,
                189.3
            ),
            new SnapshotItemModel(
                "Stable Config",
                "稳定配置完成，准备长期使用",
                "2025-04-01 11:00",
                "1.20.4",
                "Fabric Loader 0.16.0",
                35,
                52,
                201.8
            ),
        };

        foreach (var item in items)
        {
            Snapshots.Add(item);
        }

        return Task.CompletedTask;
    }
}

public class SnapshotItemModel(
    string label,
    string remark,
    string createdAt,
    string gameVersion,
    string loader,
    int packageCount,
    int fileCount,
    double totalSize)
{
    public string Label { get; } = label;
    public string Remark { get; } = remark;
    public string CreatedAt { get; } = createdAt;
    public string GameVersion { get; } = gameVersion;
    public string Loader { get; } = loader;
    public int PackageCount { get; } = packageCount;
    public int FileCount { get; } = fileCount;
    public double TotalSize { get; } = totalSize;
    public string TotalSizeFormatted => $"{TotalSize:F1} MB";

    public ObservableCollection<ReferenceItemModel> References { get; } =
    [
        new("mods/sodium.jar", ".cache/abc123"),
        new("mods/lithium.jar", ".cache/def456"),
        new("mods/iris.jar", ".cache/ghi789"),
        new("mods/fabric-api.jar", ".cache/jkl012"),
        new("mods/modmenu.jar", ".cache/mno345"),
        new("config/sodium-options.json", ".cache/pqr678"),
        new("shaderpacks/Complementary.v4.zip", ".cache/stu901"),
    ];
}

public class ReferenceItemModel(string relativePath, string target)
{
    public string RelativePath { get; } = relativePath;
    public string Target { get; } = target;
}
