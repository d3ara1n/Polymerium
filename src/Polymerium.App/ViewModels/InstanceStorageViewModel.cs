using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.App.Facilities;
using Trident.Abstractions;
using Trident.Core.Services;

namespace Polymerium.App.ViewModels;

public partial class InstanceStorageViewModel(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager)
    : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Reactive Properties

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
    public partial ulong OptionsSize { get; set; }

    [ObservableProperty]
    public partial ulong OtherSize { get; set; }

    [ObservableProperty]
    public partial bool HasCleanupSuggestions { get; set; }

    [ObservableProperty]
    public partial bool ShouldCleanLogs { get; set; }

    [ObservableProperty]
    public partial bool ShouldCleanCrashReports { get; set; }

    #endregion

    protected override async Task OnInitializeAsync() => await Task.Run(CalculateAsync);

    #region Calculation

    private Task CalculateAsync()
    {
        var homeDir = PathDef.Default.DirectoryOfHome(Basic.Key);
        var liveDir = PathDef.Default.DirectoryOfLive(Basic.Key);

        // Calculate main categories
        (ModsSize, ModsCount) = CalculateDirectorySize(Path.Combine(liveDir, "mods"));
        (ResourcePacksSize, ResourcePacksCount) = CalculateDirectorySize(Path.Combine(liveDir, "resourcepacks"));
        (ShaderPacksSize, ShaderPacksCount) = CalculateDirectorySize(Path.Combine(liveDir, "shaderpacks"));
        (WorldsSize, WorldsCount) = CalculateDirectorySize(Path.Combine(liveDir, "saves"));

        // Calculate additional folders
        (ScreenshotsSize, ScreenshotsCount) = CalculateDirectorySize(Path.Combine(liveDir, "screenshots"));
        (LogsSize, LogsCount) = CalculateDirectorySize(Path.Combine(liveDir, "logs"));
        (CrashReportsSize, CrashReportsCount) = CalculateDirectorySize(Path.Combine(liveDir, "crash-reports"));
        (ConfigSize, ConfigCount) = CalculateDirectorySize(Path.Combine(liveDir, "config"));

        // Calculate options and servers (small files)
        var optionsSize = 0ul;
        var optionsFile = Path.Combine(liveDir, "options.txt");
        var serversFile = Path.Combine(liveDir, "servers.dat");
        if (File.Exists(optionsFile))
        {
            optionsSize += (ulong)new FileInfo(optionsFile).Length;
        }
        if (File.Exists(serversFile))
        {
            optionsSize += (ulong)new FileInfo(serversFile).Length;
        }
        OptionsSize = optionsSize;

        // Calculate total
        var calculatedTotal = ModsSize + ResourcePacksSize + ShaderPacksSize + WorldsSize +
                             ScreenshotsSize + LogsSize + CrashReportsSize + ConfigSize + OptionsSize;

        // Calculate other files (total directory size minus calculated categories)
        var (totalDirSize, _) = CalculateDirectorySize(homeDir);
        TotalSize = totalDirSize;
        OtherSize = totalDirSize > calculatedTotal ? totalDirSize - calculatedTotal : 0;

        // Determine cleanup suggestions
        ShouldCleanLogs = LogsSize > 10 * 1024 * 1024; // > 10 MiB
        ShouldCleanCrashReports = CrashReportsSize > 5 * 1024 * 1024; // > 5 MiB
        HasCleanupSuggestions = ShouldCleanLogs || ShouldCleanCrashReports;

        return Task.CompletedTask;
    }

    private static (ulong size, ulong count) CalculateDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return (0ul, 0ul);
        }

        try
        {
            var directory = new DirectoryInfo(path);
            var (size, count) = directory
                               .GetFiles()
                               .Aggregate((0ul, 0ul),
                                          (current, file) => (current.Item1 + (ulong)file.Length, current.Item2 + 1));
            return directory
                  .GetDirectories()
                  .Aggregate((size, count),
                             (current, dir) =>
                             {
                                 var (subSize, subCount) = CalculateDirectorySize(dir.FullName);
                                 return (current.size + subSize, current.count + subCount);
                             });
        }
        catch
        {
            return (0ul, 0ul);
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenInstanceFolder()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        if (Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private void OpenFolder(string folderName)
    {
        var liveDir = PathDef.Default.DirectoryOfLive(Basic.Key);
        var dir = Path.Combine(liveDir, folderName);
        if (Directory.Exists(dir))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    private async Task CleanLogs()
    {
        var liveDir = PathDef.Default.DirectoryOfLive(Basic.Key);
        var logsDir = Path.Combine(liveDir, "logs");
        if (Directory.Exists(logsDir))
        {
            try
            {
                // Keep only the latest.log file
                var files = Directory.GetFiles(logsDir);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (fileName != "latest.log")
                    {
                        File.Delete(file);
                    }
                }
                await CalculateAsync();
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    [RelayCommand]
    private async Task CleanCrashReports()
    {
        var liveDir = PathDef.Default.DirectoryOfLive(Basic.Key);
        var crashDir = Path.Combine(liveDir, "crash-reports");
        if (Directory.Exists(crashDir))
        {
            try
            {
                Directory.Delete(crashDir, true);
                await CalculateAsync();
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    #endregion
}
