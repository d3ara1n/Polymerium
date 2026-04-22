using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Models;
using Polymerium.App.Facilities;
using Polymerium.App.Utilities;
using Trident.Abstractions;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.PageModels;

public partial class InstanceStoragePageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceManager instanceManager,
    ProfileManager profileManager
) : InstancePageModelBase(context, instanceManager, profileManager)
{
    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token) =>
        await Task.Run(Calculate, token);

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    [ObservableProperty]
    public partial ulong TotalSize { get; set; }

    [ObservableProperty]
    public partial ulong ModsSize { get; set; }

    [ObservableProperty]
    public partial ulong ModsCount { get; set; }

    [ObservableProperty]
    public partial ulong ResourcePacksSize { get; set; }

    [ObservableProperty]
    public partial ulong ResourcePacksCount { get; set; }

    [ObservableProperty]
    public partial ulong ShaderPacksSize { get; set; }

    [ObservableProperty]
    public partial ulong ShaderPacksCount { get; set; }

    [ObservableProperty]
    public partial ulong WorldsSize { get; set; }

    [ObservableProperty]
    public partial ulong WorldsCount { get; set; }

    [ObservableProperty]
    public partial ulong ScreenshotsSize { get; set; }

    [ObservableProperty]
    public partial ulong ScreenshotsCount { get; set; }

    [ObservableProperty]
    public partial ulong LogsSize { get; set; }

    [ObservableProperty]
    public partial ulong LogsCount { get; set; }

    [ObservableProperty]
    public partial ulong CrashReportsSize { get; set; }

    [ObservableProperty]
    public partial ulong CrashReportsCount { get; set; }

    [ObservableProperty]
    public partial ulong ConfigSize { get; set; }

    [ObservableProperty]
    public partial ulong ConfigCount { get; set; }

    [ObservableProperty]
    public partial ulong OtherSize { get; set; }

    [ObservableProperty]
    public partial ulong OtherCount { get; set; }

    #endregion

    #region Other

    private void Calculate()
    {
        var homeDir = PathDef.Default.DirectoryOfHome(Basic.Key);

        // Calculate main categories
        (ModsSize, ModsCount) = CalculateDirectorySize("mods");
        (ResourcePacksSize, ResourcePacksCount) = CalculateDirectorySize("resourcepacks");
        (ShaderPacksSize, ShaderPacksCount) = CalculateDirectorySize("shaderpacks");
        (WorldsSize, WorldsCount) = CalculateDirectorySize("saves");

        // Calculate additional folders
        (ScreenshotsSize, ScreenshotsCount) = CalculateDirectorySize("screenshots");
        (LogsSize, LogsCount) = CalculateDirectorySize("logs");
        (CrashReportsSize, CrashReportsCount) = CalculateDirectorySize("crash-reports");
        (ConfigSize, ConfigCount) = CalculateDirectorySize("config");

        // Calculate total
        var calculatedTotalSize =
            ModsSize
            + ResourcePacksSize
            + ShaderPacksSize
            + WorldsSize
            + ScreenshotsSize
            + LogsSize
            + CrashReportsSize
            + ConfigSize;

        var calculatedTotalCount =
            ModsCount
            + ResourcePacksCount
            + ShaderPacksCount
            + WorldsCount
            + ScreenshotsCount
            + LogsCount
            + CrashReportsCount
            + ConfigCount;

        // Calculate other files (total directory size minus calculated categories)
        var (totalDirSize, totalDirCount) = CalculateDirectorySize(homeDir);
        TotalSize = totalDirSize;
        OtherSize = totalDirSize > calculatedTotalSize ? totalDirSize - calculatedTotalSize : 0;
        OtherCount =
            totalDirCount > calculatedTotalCount ? totalDirCount - calculatedTotalCount : 0;

        IsLoading = false;
    }

    private (ulong, ulong) CalculateDirectorySize(string folderName)
    {
        var buildDir = PathDef.Default.DirectoryOfBuild(Basic.Key);
        var importDir = PathDef.Default.DirectoryOfImport(Basic.Key);
        var persistDir = PathDef.Default.DirectoryOfPersist(Basic.Key);

        var agg = new[]
        {
            FileHelper.CalculateDirectorySize(Path.Combine(buildDir, folderName)),
            FileHelper.CalculateDirectorySize(Path.Combine(importDir, folderName)),
            FileHelper.CalculateDirectorySize(Path.Combine(persistDir, folderName)),
        };

        return agg.Aggregate((0ul, 0ul), (acc, t) => (acc.Item1 + t.Item1, acc.Item2 + t.Item2));
    }

    #endregion

    #region Commands

    [RelayCommand]
    private Task OpenInstanceFolder()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        if (Directory.Exists(dir))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(
                TopLevel.GetTopLevel(MainWindow.Instance),
                new(dir),
                "Failed to open instance folder"
            );
        }

        return Task.CompletedTask;
    }

    private bool CanOpenBuildFolder(string folderName) =>
        Directory.Exists(Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), folderName));

    [RelayCommand(CanExecute = nameof(CanOpenBuildFolder))]
    private Task OpenBuildFolder(string folderName)
    {
        var buildDir = PathDef.Default.DirectoryOfBuild(Basic.Key);
        var dir = Path.Combine(buildDir, folderName);
        if (Directory.Exists(dir))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(
                TopLevel.GetTopLevel(MainWindow.Instance),
                new(dir),
                $"Failed to open {folderName} folder"
            );
        }

        return Task.CompletedTask;
    }

    private bool CanOpenImportFolder(string folderName) =>
        Directory.Exists(Path.Combine(PathDef.Default.DirectoryOfImport(Basic.Key), folderName));

    [RelayCommand(CanExecute = nameof(CanOpenImportFolder))]
    private Task OpenImportFolder(string folderName)
    {
        var import = PathDef.Default.DirectoryOfImport(Basic.Key);
        var dir = Path.Combine(import, folderName);
        if (Directory.Exists(dir))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(
                TopLevel.GetTopLevel(MainWindow.Instance),
                new(dir),
                $"Failed to open {folderName} folder"
            );
        }

        return Task.CompletedTask;
    }

    private bool CanOpenPersistFolder(string folderName) =>
        Directory.Exists(Path.Combine(PathDef.Default.DirectoryOfPersist(Basic.Key), folderName));

    [RelayCommand(CanExecute = nameof(CanOpenPersistFolder))]
    private Task OpenPersistFolder(string folderName)
    {
        var persistDir = PathDef.Default.DirectoryOfPersist(Basic.Key);
        var dir = Path.Combine(persistDir, folderName);
        if (Directory.Exists(dir))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(
                TopLevel.GetTopLevel(MainWindow.Instance),
                new(dir),
                $"Failed to open {folderName} folder"
            );
        }

        return Task.CompletedTask;
    }

    #endregion
}
