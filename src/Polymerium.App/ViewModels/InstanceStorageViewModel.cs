using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Trident.Abstractions;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceStorageViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Reactive

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

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync() => await Task.Run(Calculate);

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
        var calculatedTotal = ModsSize
                            + ResourcePacksSize
                            + ShaderPacksSize
                            + WorldsSize
                            + ScreenshotsSize
                            + LogsSize
                            + CrashReportsSize
                            + ConfigSize;

        // Calculate other files (total directory size minus calculated categories)
        var (totalDirSize, _) = CalculateDirectorySize(homeDir);
        TotalSize = totalDirSize;
        OtherSize = totalDirSize > calculatedTotal ? totalDirSize - calculatedTotal : 0;
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
            FileHelper.CalculateDirectorySize(Path.Combine(persistDir, folderName))
        };

        return agg.Aggregate((0ul, 0ul), (acc, t) => (acc.Item1 + t.Item1, acc.Item2 + t.Item2));
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenInstanceFolder()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        if (Directory.Exists(dir))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
        }
    }

    private bool CanOpenBuildFolder(string folderName) =>
        Directory.Exists(Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), folderName));

    [RelayCommand(CanExecute = nameof(CanOpenBuildFolder))]
    private void OpenBuildFolder(string folderName)
    {
        var buildDir = PathDef.Default.DirectoryOfBuild(Basic.Key);
        var dir = Path.Combine(buildDir, folderName);
        if (Directory.Exists(dir))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
        }
    }

    private bool CanOpenImportFolder(string folderName) =>
        Directory.Exists(Path.Combine(PathDef.Default.DirectoryOfImport(Basic.Key), folderName));

    [RelayCommand(CanExecute = nameof(CanOpenImportFolder))]
    private void OpenImportFolder(string folderName)
    {
        var import = PathDef.Default.DirectoryOfImport(Basic.Key);
        var dir = Path.Combine(import, folderName);
        if (Directory.Exists(dir))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
        }
    }

    private bool CanOpenPersistFolder(string folderName) =>
        Directory.Exists(Path.Combine(PathDef.Default.DirectoryOfPersist(Basic.Key), folderName));

    [RelayCommand(CanExecute = nameof(CanOpenPersistFolder))]
    private void OpenPersistFolder(string folderName)
    {
        var persistDir = PathDef.Default.DirectoryOfPersist(Basic.Key);
        var dir = Path.Combine(persistDir, folderName);
        if (Directory.Exists(dir))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
        }
    }

    #endregion
}
