using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;
using Polymerium.App.Properties;

namespace Polymerium.App.Modals;

public partial class GameCrashReportModal : Modal
{
    public static readonly StyledProperty<CrashReportModel?> ReportProperty =
        AvaloniaProperty.Register<GameCrashReportModal, CrashReportModel?>(nameof(Report));

    public GameCrashReportModal()
    {
        InitializeComponent();
    }

    public CrashReportModel? Report
    {
        get => GetValue(ReportProperty);
        set => SetValue(ReportProperty, value);
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (Report == null)
            return;

        var text = GenerateCrashReportText();
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text);
    }

    [RelayCommand]
    private void OpenLogFile()
    {
        if (Report?.LogFilePath != null && File.Exists(Report.LogFilePath))
        {
            TopLevel.GetTopLevel(this)?.Launcher.LaunchFileInfoAsync(new FileInfo(Report.LogFilePath));
        }
    }

    [RelayCommand]
    private void OpenCrashReport()
    {
        if (Report?.CrashReportPath != null && File.Exists(Report.CrashReportPath))
        {
            TopLevel.GetTopLevel(this)?.Launcher.LaunchFileInfoAsync(new FileInfo(Report.CrashReportPath));
        }
    }

    [RelayCommand]
    private void OpenGameDirectory()
    {
        if (Report?.GameDirectory != null && Directory.Exists(Report.GameDirectory))
        {
            TopLevel.GetTopLevel(this)?.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(Report.GameDirectory));
        }
    }

    [RelayCommand]
    private async Task ExportDiagnosticPackageAsync()
    {
        if (Report == null)
            return;

        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null)
            return;

        try
        {
            var fileName = $"crash-diagnostic-{Report.InstanceKey}-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var file = await top.StorageProvider.SaveFilePickerAsync(new()
            {
                Title = Properties.Resources
                                  .GameCrashReportModal_ExportDialogTitle,
                SuggestedFileName = fileName,
                DefaultExtension = "zip",
                FileTypeChoices =
                [
                    new("Zip Archive")
                    {
                        Patterns = ["*.zip"]
                    }
                ]
            });

            if (file == null)
                return;

            await using var zipStream = await file.OpenWriteAsync();
            await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            // Add crash report summary
            var summaryEntry = archive.CreateEntry("crash-report.txt");
            await using (var writer = new StreamWriter(await summaryEntry.OpenAsync()))
            {
                await writer.WriteAsync(GenerateCrashReportText());
            }

            // Add command line
            if (Report.CommandLine != null)
            {
                var commandLineEntry = archive.CreateEntry("command-line.txt");
                await using var writer = new StreamWriter(await commandLineEntry.OpenAsync());
                await writer.WriteAsync(Report.CommandLine);
            }

            // Add debug.log (not latest.log)
            var debugLogPath = Path.Combine(Report.GameDirectory, "logs", "debug.log");
            if (File.Exists(debugLogPath))
            {
                await archive.CreateEntryFromFileAsync(debugLogPath, "logs/debug.log");
            }

            // Add latest.log as fallback
            var latestLogPath = Path.Combine(Report.GameDirectory, "logs", "latest.log");
            if (File.Exists(latestLogPath))
            {
                await archive.CreateEntryFromFileAsync(latestLogPath, "logs/latest.log");
            }

            // Add all crash report files from crash-reports directory
            if (Report.CrashReportPath != null)
            {
                var crashFileName = Path.GetFileName(Report.CrashReportPath);
                await archive.CreateEntryFromFileAsync(Report.CrashReportPath, $"crash-reports/{crashFileName}");
            }

            // Add profile.json from instance directory
            var profilePath = Path.Combine(Path.GetDirectoryName(Report.GameDirectory) ?? "", "profile.json");
            if (File.Exists(profilePath))
            {
                await archive.CreateEntryFromFileAsync(profilePath, "profile.json");
            }

            // Add options.txt
            var optionsPath = Path.Combine(Report.GameDirectory, "options.txt");
            if (File.Exists(optionsPath))
            {
                await archive.CreateEntryFromFileAsync(optionsPath, "options.txt");
            }

            // Add mods list
            var modsDir = Path.Combine(Report.GameDirectory, "mods");
            if (Directory.Exists(modsDir))
            {
                var modsListEntry = archive.CreateEntry("mods-list.txt");
                await using var writer = new StreamWriter(await modsListEntry.OpenAsync());
                var modFiles = Directory.GetFiles(modsDir, "*.jar").Select(Path.GetFileName).OrderBy(x => x);
                foreach (var mod in modFiles)
                {
                    await writer.WriteLineAsync(mod);
                }
            }

            // Add hs_err_pid files if they exist (JVM crash logs)
            var hsErrFiles = Directory.GetFiles(Report.GameDirectory, "hs_err_pid*.log");
            foreach (var hsErrFile in hsErrFiles.Take(3)) // Limit to 3 most recent
            {
                var hsFileName = Path.GetFileName(hsErrFile);
                await archive.CreateEntryFromFileAsync(hsErrFile, $"jvm-crashes/{hsFileName}");
            }
        }
        catch (Exception)
        {
            // Silently fail or show error notification
        }
    }

    private string GenerateCrashReportText()
    {
        if (Report == null)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("=== GAME CRASH DIAGNOSTIC REPORT ===");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("--- Crash Summary ---");
        sb.AppendLine($"Instance: {Report.InstanceName} ({Report.InstanceKey})");
        sb.AppendLine($"Launch Time: {Report.LaunchTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Crash Time: {Report.CrashTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Exit Code: {Report.ExitCode}");
        sb.AppendLine($"Error Message: {Report.ExceptionMessage}");
        sb.AppendLine();
        sb.AppendLine("--- Game Information ---");
        sb.AppendLine($"Minecraft Version: {Report.MinecraftVersion}");
        sb.AppendLine($"Mod Loader: {Report.LoaderLabel}");

        sb.AppendLine($"Game Directory: {Report.GameDirectory}");
        if (Report.ModCount > 0)
        {
            sb.AppendLine($"Installed Mods: {Report.ModCount}");
        }

        sb.AppendLine();
        sb.AppendLine("--- System Information ---");
        sb.AppendLine($"Operating System: {Report.OperatingSystem}");
        sb.AppendLine($"Java Version: {Report.JavaVersion}");
        sb.AppendLine($"Java Path: {Report.JavaPath}");
        if (!string.IsNullOrEmpty(Report.AllocatedMemory))
        {
            sb.AppendLine($"Allocated Memory: {Report.AllocatedMemory}");
        }

        sb.AppendLine();
        if (!string.IsNullOrEmpty(Report.LastLogLines))
        {
            sb.AppendLine("--- Last Log Lines (from latest.log) ---");
            sb.AppendLine(Report.LastLogLines);
            sb.AppendLine();
        }

        sb.AppendLine("--- File Locations ---");
        if (!string.IsNullOrEmpty(Report.LogFilePath))
        {
            sb.AppendLine($"Log File: {Report.LogFilePath}");
        }

        if (!string.IsNullOrEmpty(Report.CrashReportPath))
        {
            sb.AppendLine($"Crash Report: {Report.CrashReportPath}");
        }

        return sb.ToString();
    }
}
