using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Refit;
using Trident.Abstractions;
using Trident.Core.Clients;
using Trident.Core.Models.MclogsApi;

namespace Polymerium.App.Modals;

public partial class GameCrashReportModal : Modal
{
    private const int MaxUploadLines = 10_000;
    private const int MaxUploadBytes = 10 * 1024 * 1024 - 1;
    private const int MaxCrashReportUploadLines = 25_000;

    private static readonly Uri AiTemplateUri = new(
        "avares://Polymerium.App/Assets/Templates/CrashAiAnalysis.md"
    );

    private readonly IMclogsClient? _mclogsClient = Program.Services?.GetService<IMclogsClient>();
    private readonly NotificationService? _notificationService =
        Program.Services?.GetService<NotificationService>();

    public static readonly StyledProperty<CrashReportModel?> ReportProperty =
        AvaloniaProperty.Register<GameCrashReportModal, CrashReportModel?>(nameof(Report));

    public GameCrashReportModal() => InitializeComponent();

    public CrashReportModel? Report
    {
        get => GetValue(ReportProperty);
        set => SetValue(ReportProperty, value);
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        if (Report == null)
        {
            return;
        }

        var text = GenerateCrashReportText();
        TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text);
    }

    [RelayCommand]
    private void OpenLogFile()
    {
        if (Report?.LogFilePath != null && File.Exists(Report.LogFilePath))
        {
            TopLevel.GetTopLevel(this)?.Launcher.LaunchFileInfoAsync(new(Report.LogFilePath));
        }
    }

    [RelayCommand]
    private void OpenCrashReport()
    {
        if (Report?.CrashReportPath != null && File.Exists(Report.CrashReportPath))
        {
            TopLevel.GetTopLevel(this)?.Launcher.LaunchFileInfoAsync(new(Report.CrashReportPath));
        }
    }

    [RelayCommand]
    private void OpenGameDirectory()
    {
        if (Report?.GameDirectory != null && Directory.Exists(Report.GameDirectory))
        {
            TopLevel
                .GetTopLevel(this)
                ?.Launcher.LaunchDirectoryInfoAsync(new(Report.GameDirectory));
        }
    }

    [RelayCommand]
    private async Task ExportDiagnosticPackageAsync()
    {
        if (Report == null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null)
        {
            return;
        }

        try
        {
            var fileName =
                $"crash-diagnostic-{Report.InstanceKey}-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var file = await top.StorageProvider.SaveFilePickerAsync(
                new()
                {
                    Title = Properties.Resources.GameCrashReportModal_ExportDialogTitle,
                    SuggestedFileName = fileName,
                    SuggestedStartLocation = await top.StorageProvider.TryGetWellKnownFolderAsync(
                        WellKnownFolder.Downloads
                    ),
                    DefaultExtension = "zip",
                    FileTypeChoices = [new("Zip Archive") { Patterns = ["*.zip"] }],
                }
            );

            if (file == null)
            {
                return;
            }

            await using var zipStream = await file.OpenWriteAsync();
            await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            var summaryEntry = archive.CreateEntry("crash-report.txt");
            await using (var writer = new StreamWriter(await summaryEntry.OpenAsync()))
            {
                await writer.WriteAsync(GenerateCrashReportText());
            }

            if (Report.CommandLine != null)
            {
                var commandLineEntry = archive.CreateEntry("command-line.txt");
                await using var writer = new StreamWriter(await commandLineEntry.OpenAsync());
                await writer.WriteAsync(Report.CommandLine);
            }

            var debugLogPath = Path.Combine(Report.GameDirectory, "logs", "debug.log");
            if (File.Exists(debugLogPath))
            {
                await archive.CreateEntryFromFileAsync(debugLogPath, "logs/debug.log");
            }

            var latestLogPath = Path.Combine(Report.GameDirectory, "logs", "latest.log");
            if (File.Exists(latestLogPath))
            {
                await archive.CreateEntryFromFileAsync(latestLogPath, "logs/latest.log");
            }

            if (Report.CrashReportPath != null)
            {
                var crashFileName = Path.GetFileName(Report.CrashReportPath);
                await archive.CreateEntryFromFileAsync(
                    Report.CrashReportPath,
                    $"crash-reports/{crashFileName}"
                );
            }

            var profilePath = Path.Combine(
                Path.GetDirectoryName(Report.GameDirectory) ?? string.Empty,
                "profile.json"
            );
            if (File.Exists(profilePath))
            {
                await archive.CreateEntryFromFileAsync(profilePath, "profile.json");
            }

            var optionsPath = Path.Combine(Report.GameDirectory, "options.txt");
            if (File.Exists(optionsPath))
            {
                await archive.CreateEntryFromFileAsync(optionsPath, "options.txt");
            }

            var modsDir = Path.Combine(Report.GameDirectory, "mods");
            if (Directory.Exists(modsDir))
            {
                var modsListEntry = archive.CreateEntry("mods-list.txt");
                await using var writer = new StreamWriter(await modsListEntry.OpenAsync());
                var modFiles = Directory
                    .GetFiles(modsDir, "*.jar")
                    .Select(Path.GetFileName)
                    .OrderBy(x => x);
                foreach (var mod in modFiles)
                {
                    await writer.WriteLineAsync(mod);
                }
            }

            var hsErrFiles = Directory.GetFiles(Report.GameDirectory, "hs_err_pid*.log");
            foreach (var hsErrFile in hsErrFiles.Take(3))
            {
                var hsFileName = Path.GetFileName(hsErrFile);
                await archive.CreateEntryFromFileAsync(hsErrFile, $"jvm-crashes/{hsFileName}");
            }
        }
        catch
        {
            // Ignore export failures for now.
        }
    }

    [RelayCommand]
    private async Task ExportAnalysisPackageAsync()
    {
        if (Report == null)
        {
            return;
        }

        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider == null)
        {
            return;
        }

        var logContent = GetBestLogContentForAiExport();
        var crashReportContent = GetCrashReportContentForAiExport();
        if (string.IsNullOrWhiteSpace(logContent) && string.IsNullOrWhiteSpace(crashReportContent))
        {
            _notificationService?.PopMessage(
                Properties.Resources.GameCrashReportModal_AiExportLogUnavailableMessage,
                Properties.Resources.GameCrashReportModal_AiExportLogUnavailableTitle,
                GrowlLevel.Danger
            );
            return;
        }

        try
        {
            CreateLogResponse? crashReportUpload = null;
            CreateLogResponse? logUpload = null;
            using (
                var uploadProgress = _notificationService?.PopProgress(
                    Properties.Resources.GameCrashReportModal_AiExportUploadingMessage,
                    Properties.Resources.GameCrashReportModal_AiExportUploadingTitle,
                    GrowlLevel.Information
                )
            )
            {
                if (!string.IsNullOrWhiteSpace(crashReportContent))
                {
                    crashReportUpload = await UploadTextToMclogsAsync(
                        crashReportContent,
                        "crash-report"
                    );
                }

                if (!string.IsNullOrWhiteSpace(logContent))
                {
                    logUpload = await UploadTextToMclogsAsync(logContent, "latest-log");
                }
            }

            if (
                (crashReportUpload == null || !crashReportUpload.Success)
                && (logUpload == null || !logUpload.Success)
            )
            {
                _notificationService?.PopMessage(
                    crashReportUpload?.Error
                        ?? logUpload?.Error
                        ?? Properties.Resources.GameCrashReportModal_AiExportUploadFailedMessage,
                    Properties.Resources.GameCrashReportModal_AiExportUploadFailedTitle,
                    GrowlLevel.Danger
                );
                return;
            }

            var template = await LoadAiAnalysisTemplateAsync();
            var markdown = GenerateAiAnalysisMarkdown(
                template,
                crashReportUpload,
                logUpload,
                crashReportContent,
                logContent
            );
            var fileName =
                $"crash-ai-analysis-{Report.InstanceKey}-{DateTime.Now:yyyyMMdd-HHmmss}.md";
            var file = await top.StorageProvider.SaveFilePickerAsync(
                new()
                {
                    Title = Properties.Resources.GameCrashReportModal_AiExportDialogTitle,
                    SuggestedFileName = fileName,
                    SuggestedStartLocation = await top.StorageProvider.TryGetWellKnownFolderAsync(
                        WellKnownFolder.Downloads
                    ),
                    DefaultExtension = "md",
                    FileTypeChoices = [new("Markdown") { Patterns = ["*.md"] }],
                }
            );

            if (file == null)
            {
                return;
            }

            NotificationService.ProgressHandle? exportProgress = null;
            try
            {
                exportProgress = _notificationService?.PopProgress(
                    Properties.Resources.GameCrashReportModal_AiExportWritingMessage,
                    Properties.Resources.GameCrashReportModal_AiExportWritingTitle,
                    GrowlLevel.Information
                );

                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteAsync(markdown);
            }
            finally
            {
                exportProgress?.Dispose();
            }

            _notificationService?.PopMessage(
                Properties.Resources.GameCrashReportModal_AiExportSuccessMessage,
                Properties.Resources.GameCrashReportModal_AiExportSuccessTitle,
                GrowlLevel.Success,
                forceExpire: true
            );
        }
        catch (Exception ex)
        {
            _notificationService?.PopMessage(
                Program.IsDebug ? ex.ToString() : ex.Message,
                Properties.Resources.GameCrashReportModal_AiExportFailedTitle,
                GrowlLevel.Danger
            );
        }
    }

    private async Task<string> LoadAiAnalysisTemplateAsync()
    {
        await using var stream = AssetLoader.Open(AiTemplateUri);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return await reader.ReadToEndAsync();
    }

    private async Task<CreateLogResponse?> UploadTextToMclogsAsync(
        string content,
        string contentKind
    )
    {
        if (_mclogsClient == null)
        {
            return new(false, null, null, null, "mclo.gs client service is unavailable.");
        }

        try
        {
            return await _mclogsClient.CreateLogAsync(
                new CreateLogRequest(
                    content,
                    $"{Program.Brand}/{Program.Version}",
                    [
                        new("content_kind", contentKind, "Content Kind", false),
                        new("instance_name", Report?.InstanceName, "Instance Name", true),
                        new("instance_key", Report?.InstanceKey, "Instance Key", false),
                        new(
                            "minecraft_version",
                            Report?.MinecraftVersion,
                            "Minecraft Version",
                            true
                        ),
                        new("loader", Report?.LoaderLabel, "Loader", true),
                        new("operating_system", Report?.OperatingSystem, "Operating System", true),
                    ]
                )
            );
        }
        catch (ApiException ex)
        {
            return new(false, null, null, null, ex.Message);
        }
        catch (Exception ex)
        {
            return new(false, null, null, null, ex.Message);
        }
    }

    private string GenerateAiAnalysisMarkdown(
        string template,
        CreateLogResponse? crashReportUpload,
        CreateLogResponse? logUpload,
        string? crashReportContent,
        string? uploadedLogContent
    )
    {
        var markdown = template;
        foreach (
            var pair in BuildAiTemplateTokens(
                crashReportUpload,
                logUpload,
                crashReportContent,
                uploadedLogContent
            )
        )
        {
            markdown = markdown.Replace(
                $"{{{{{pair.Key}}}}}",
                pair.Value,
                StringComparison.Ordinal
            );
        }

        return markdown;
    }

    private Dictionary<string, string> BuildAiTemplateTokens(
        CreateLogResponse? crashReportUpload,
        CreateLogResponse? logUpload,
        string? crashReportContent,
        string? uploadedLogContent
    )
    {
        var avaloniaVersion =
            typeof(Application).Assembly.GetName().Version?.ToString() ?? "Unknown";

        return new(StringComparer.Ordinal)
        {
            ["generated_at"] = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            ["instance_name"] = Report?.InstanceName ?? "Unknown",
            ["instance_key"] = Report?.InstanceKey ?? "Unknown",
            ["launch_time"] = Report?.LaunchTime.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "Unknown",
            ["crash_time"] = Report?.CrashTime.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "Unknown",
            ["exit_code"] = Report?.ExitCode.ToString(CultureInfo.InvariantCulture) ?? "Unknown",
            ["exception_message"] = EscapeMarkdownInline(Report?.ExceptionMessage),
            ["play_time"] = Report?.PlayTime ?? "Unknown",
            ["minecraft_version"] = Report?.MinecraftVersion ?? "Unknown",
            ["loader"] = Report?.LoaderLabel ?? "Unknown",
            ["mod_count"] = Report?.ModCount.ToString(CultureInfo.InvariantCulture) ?? "0",
            ["game_directory"] = EscapeMarkdownInline(Report?.GameDirectory),
            ["polymerium_version"] = Program.Version,
            ["build_configuration"] = Program.IsDebug ? "Debug" : "Release",
            ["ui_language"] = CultureInfo.CurrentUICulture.Name,
            ["operating_system"] = EscapeMarkdownInline(Report?.OperatingSystem),
            ["installed_memory"] = EscapeMarkdownInline(Report?.InstalledMemory),
            ["allocated_memory"] = EscapeMarkdownInline(Report?.AllocatedMemory),
            ["java_version"] = EscapeMarkdownInline(Report?.JavaVersion),
            ["java_path"] = EscapeMarkdownInline(Report?.JavaPath),
            ["dotnet_runtime"] = RuntimeInformation.FrameworkDescription,
            ["avalonia_version"] = avaloniaVersion,
            ["crash_report_url"] = crashReportUpload?.Url ?? "Unavailable",
            ["crash_report_raw_url"] =
                crashReportUpload?.Raw ?? crashReportUpload?.Url ?? "Unavailable",
            ["log_url"] = logUpload?.Url ?? "Unavailable",
            ["raw_log_url"] = logUpload?.Raw ?? logUpload?.Url ?? "Unavailable",
            ["log_file_path"] = EscapeMarkdownInline(Report?.LogFilePath),
            ["crash_report_path"] = EscapeMarkdownInline(Report?.CrashReportPath),
            ["command_line"] = EscapeCodeFenceContent(Report?.CommandLine),
            ["crash_report_excerpt"] = EscapeCodeFenceContent(
                GetPreviewCrashReport(crashReportContent)
            ),
            ["last_log_lines"] = EscapeCodeFenceContent(GetPreviewLogLines(uploadedLogContent)),
        };
    }

    private string? GetBestLogContentForAiExport()
    {
        if (Report == null)
        {
            return null;
        }

        var candidatePaths = new[]
        {
            Path.Combine(Report.GameDirectory, "logs", "debug.log"),
            Report.LogFilePath,
            Path.Combine(Report.GameDirectory, "logs", "latest.log"),
        };

        foreach (
            var candidatePath in candidatePaths.Where(static x => !string.IsNullOrWhiteSpace(x))
        )
        {
            try
            {
                if (candidatePath != null && File.Exists(candidatePath))
                {
                    var content = BuildTailLogContent(File.ReadLines(candidatePath));
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        return content;
                    }
                }
            }
            catch
            {
                // Ignore and try the next candidate.
            }
        }

        return string.IsNullOrWhiteSpace(Report.LastLogLines)
            ? null
            : BuildTailLogContent(
                Report.LastLogLines.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            );
    }

    private string? GetCrashReportContentForAiExport()
    {
        if (
            string.IsNullOrWhiteSpace(Report?.CrashReportPath)
            || !File.Exists(Report.CrashReportPath)
        )
        {
            return null;
        }

        try
        {
            return BuildHeadContent(
                File.ReadLines(Report.CrashReportPath),
                MaxCrashReportUploadLines
            );
        }
        catch
        {
            return null;
        }
    }

    private static string? BuildTailLogContent(IEnumerable<string> lines)
    {
        var tail = new Queue<string>(MaxUploadLines);
        foreach (var line in lines)
        {
            if (tail.Count == MaxUploadLines)
            {
                tail.Dequeue();
            }

            tail.Enqueue(line);
        }

        if (tail.Count == 0)
        {
            return null;
        }

        while (tail.Count > 0)
        {
            var content = string.Join(Environment.NewLine, tail);
            if (Encoding.UTF8.GetByteCount(content) <= MaxUploadBytes)
            {
                return content;
            }

            tail.Dequeue();
        }

        return null;
    }

    private static string? BuildHeadContent(IEnumerable<string> lines, int maxLines)
    {
        var builder = new StringBuilder();
        var count = 0;
        foreach (var line in lines)
        {
            if (count >= maxLines)
            {
                break;
            }

            var candidateLine = count == 0 ? line : $"{Environment.NewLine}{line}";
            if (
                Encoding.UTF8.GetByteCount(builder.ToString())
                    + Encoding.UTF8.GetByteCount(candidateLine)
                > MaxUploadBytes
            )
            {
                break;
            }

            builder.Append(candidateLine);
            count++;
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private static string GetPreviewLogLines(string? logContent)
    {
        if (string.IsNullOrWhiteSpace(logContent))
        {
            return "Unavailable";
        }

        var lines = logContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .TakeLast(100);
        var preview = string.Join(Environment.NewLine, lines).Trim();
        return string.IsNullOrWhiteSpace(preview) ? "Unavailable" : preview;
    }

    private static string GetPreviewCrashReport(string? crashReportContent)
    {
        if (string.IsNullOrWhiteSpace(crashReportContent))
        {
            return "Unavailable";
        }

        var lines = crashReportContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Take(100);
        var preview = string.Join(Environment.NewLine, lines).Trim();
        return string.IsNullOrWhiteSpace(preview) ? "Unavailable" : preview;
    }

    private static string EscapeMarkdownInline(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Replace("`", "\\`");

    private static string EscapeCodeFenceContent(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unavailable";
        }

        return value.Replace("```", "'''", StringComparison.Ordinal).Trim();
    }

    private string GenerateCrashReportText()
    {
        if (Report == null)
        {
            return string.Empty;
        }

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
