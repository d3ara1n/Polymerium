using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using Refit;
using TridentCore.Core.Clients;
using TridentCore.Core.Models.MclogsApi;

namespace Polymerium.Avalonia.Modals;

public partial class GameCrashReportModal : Modal
{
    private const int MAX_UPLOAD_LINES = 10_000;
    private const int MAX_UPLOAD_BYTES = 10 * 1024 * 1024 - 1;
    private const int MAX_CRASH_REPORT_UPLOAD_LINES = 25_000;

    private static readonly Uri AiTemplateUri = new("avares://Polymerium/Assets/Templates/CrashAiAnalysis.md");

    public static readonly StyledProperty<CrashReportModel?> ReportProperty =
        AvaloniaProperty.Register<GameCrashReportModal, CrashReportModel?>(nameof(Report));

    private readonly IMclogsClient? _mclogsClient = Program.Services?.GetService<IMclogsClient>();

    private readonly NotificationService? _notificationService = Program.Services?.GetService<NotificationService>();

    public GameCrashReportModal() => InitializeComponent();

    public CrashReportModel? Report
    {
        get => GetValue(ReportProperty);
        set => SetValue(ReportProperty, value);
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        if (Report == null)
        {
            return;
        }

        var text = GenerateCrashReportText();

        await TopLevelHelper.CopyToClipboardAsync(TopLevel.GetTopLevel(this),
                                                  text,
                                                  LanguageManager.Instance.GameCrashReportModal_CopyCrashReportDangerNotificationTitle.Current(),
                                                  _notificationService);
    }

    [RelayCommand]
    private Task OpenLogFile()
    {
        if (Report?.LogFilePath != null && File.Exists(Report.LogFilePath))
        {
            return TopLevelHelper.LaunchFileInfoAsync(TopLevel.GetTopLevel(this),
                                                      new(Report.LogFilePath),
                                                      LanguageManager.Instance.Shared_FailedToOpenLogFileDangerNotificationTitle.Current(),
                                                      _notificationService);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenCrashReport()
    {
        if (Report?.CrashReportPath != null && File.Exists(Report.CrashReportPath))
        {
            return TopLevelHelper.LaunchFileInfoAsync(TopLevel.GetTopLevel(this),
                                                      new(Report.CrashReportPath),
                                                      LanguageManager.Instance.GameCrashReportModal_OpenCrashReportDangerNotificationTitle.Current(),
                                                      _notificationService);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenGameDirectory()
    {
        if (Report?.GameDirectory != null && Directory.Exists(Report.GameDirectory))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevel.GetTopLevel(this),
                                                           new(Report.GameDirectory),
                                                           LanguageManager.Instance.GameCrashReportModal_OpenGameDirectoryDangerNotificationTitle.Current(),
                                                           _notificationService);
        }

        return Task.CompletedTask;
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
            var fileName = $"crash-diagnostic-{Report.InstanceKey}-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
            var file = await top.StorageProvider.SaveFilePickerAsync(new()
            {
                Title =
                    LanguageManager.Instance.GameCrashReportModal_ExportDialogTitle.Current(),
                SuggestedFileName = fileName,
                SuggestedStartLocation =
                    await top.StorageProvider
                             .TryGetWellKnownFolderAsync(WellKnownFolder
                                                            .Downloads),
                DefaultExtension = "zip",
                FileTypeChoices =
                [
                    new(LanguageManager.Instance.Shared_ZipArchiveFileTypeText.Current())
                    {
                        Patterns = ["*.zip"]
                    }
                ]
            });

            if (file == null)
            {
                return;
            }

            await using var zipStream = await file.OpenWriteAsync();
            await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
            var redactor = Redactor.Create();

            var summaryEntry = archive.CreateEntry("crash-report.txt");
            await using (var writer = new StreamWriter(await summaryEntry.OpenAsync()))
            {
                await writer.WriteAsync(redactor.Apply(GenerateCrashReportText()));
            }

            if (Report.CommandLine != null)
            {
                var commandLineEntry = archive.CreateEntry("command-line.txt");
                await using var writer = new StreamWriter(await commandLineEntry.OpenAsync());
                await writer.WriteAsync(redactor.Apply(Report.CommandLine));
            }

            await AddRedactedEntryAsync(archive,
                                        Path.Combine(Report.GameDirectory, "logs", "debug.log"),
                                        "logs/debug.log",
                                        redactor);
            await AddRedactedEntryAsync(archive,
                                        Path.Combine(Report.GameDirectory, "logs", "latest.log"),
                                        "logs/latest.log",
                                        redactor);
            if (Report.CrashReportPath != null)
            {
                await AddRedactedEntryAsync(archive,
                                            Report.CrashReportPath,
                                            $"crash-reports/{Path.GetFileName(Report.CrashReportPath)}",
                                            redactor);
            }

            var profilePath = Path.Combine(Path.GetDirectoryName(Report.GameDirectory) ?? string.Empty, "profile.json");
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
                var modFiles = Directory.GetFiles(modsDir, "*.jar").Select(Path.GetFileName).OrderBy(x => x);
                foreach (var mod in modFiles)
                {
                    await writer.WriteLineAsync(mod);
                }
            }

            foreach (var hsErrFile in Directory.GetFiles(Report.GameDirectory, "hs_err_pid*.log").Take(3))
            {
                await AddRedactedEntryAsync(archive,
                                            hsErrFile,
                                            $"jvm-crashes/{Path.GetFileName(hsErrFile)}",
                                            redactor);
            }
        }
        catch
        {
            // NOTE: 导出失败暂不阻断。
        }
    }

    // 读取文本文件，脱敏后写入 zip 条目；源文件不存在则跳过。
    private static async Task AddRedactedEntryAsync(ZipArchive archive,
                                                    string? sourcePath,
                                                    string entryName,
                                                    Redactor redactor)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            return;
        }

        var entry = archive.CreateEntry(entryName);
        await using var writer = new StreamWriter(await entry.OpenAsync());
        var content = await File.ReadAllTextAsync(sourcePath);
        await writer.WriteAsync(redactor.Apply(content));
    }

    [RelayCommand]
    private async Task ExportAnalysisPackageAsync()
    {
        if (Report == null)
        {
            return;
        }

        var overlayService = Program.Services?.GetService<OverlayService>();
        var top = TopLevel.GetTopLevel(this);
        if (overlayService == null || top?.StorageProvider == null)
        {
            return;
        }

        // 确认对话框：说明将要上传到第三方、预估内容与使用方式，不做任何实际数据收集。
        var confirmed = await overlayService.RequestConfirmationAsync(
            LanguageManager.Instance.GameCrashReportModal_AiExportConfirmBodyText.Current(),
            LanguageManager.Instance.GameCrashReportModal_AiExportConfirmTitle.Current());
        if (!confirmed)
        {
            return;
        }

        // 确定保存路径后才开始任何收集与上传。
        var fileName = $"crash-ai-analysis-{Report.InstanceKey}-{DateTime.Now:yyyyMMdd-HHmmss}.md";
        var file = await top.StorageProvider.SaveFilePickerAsync(new()
        {
            Title = LanguageManager.Instance.GameCrashReportModal_AiExportDialogTitle.Current(),
            SuggestedFileName = fileName,
            SuggestedStartLocation =
                await top.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads),
            DefaultExtension = "md",
            FileTypeChoices = [new("Markdown") { Patterns = ["*.md"] }]
        });
        if (file == null)
        {
            return;
        }

        var redactor = Redactor.Create();
        var uploaded = new List<CreateLogResponse>();

        try
        {
            // 收集原始内容，脱敏统一在出口进行：上传前对日志、写盘前对整份 Markdown。
            var logContent = GetBestLogContentForAiExport();
            var crashReportContent = GetCrashReportContentForAiExport();
            if (string.IsNullOrWhiteSpace(logContent) && string.IsNullOrWhiteSpace(crashReportContent))
            {
                _notificationService?.PopMessage(
                    LanguageManager.Instance.GameCrashReportModal_AiExportLogUnavailableMessage.Current(),
                    LanguageManager.Instance.GameCrashReportModal_AiExportLogUnavailableTitle.Current(),
                    GrowlLevel.Warning);
                return;
            }

            CreateLogResponse? crashReportUpload = null;
            CreateLogResponse? logUpload = null;
            NotificationService.ProgressHandle? uploadProgress = null;
            try
            {
                uploadProgress =
                    _notificationService?.PopProgress(
                        LanguageManager.Instance.GameCrashReportModal_AiExportUploadingMessage.Current(),
                        LanguageManager.Instance.GameCrashReportModal_AiExportUploadingTitle.Current());
                // NOTE: Action 须在 Handle 创建后挂载，构造期内引用 Handle 属提前访问。
                uploadProgress?.AddAction(new(
                    LanguageManager.Instance.Dialog_CancelButtonText.Current(),
                    new RelayCommand(() => uploadProgress?.Cancel())));
                var token = uploadProgress?.Token ?? CancellationToken.None;

                if (!string.IsNullOrWhiteSpace(crashReportContent))
                {
                    crashReportUpload = await UploadTextToMclogsAsync(redactor.Apply(crashReportContent), "crash-report", token);
                    if (crashReportUpload is { Success: true })
                    {
                        uploaded.Add(crashReportUpload);
                    }
                }

                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(logContent))
                {
                    logUpload = await UploadTextToMclogsAsync(redactor.Apply(logContent), "latest-log", token);
                    if (logUpload is { Success: true })
                    {
                        uploaded.Add(logUpload);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                await RollbackUploadsAsync(uploaded);
                return;
            }
            finally
            {
                uploadProgress?.Dispose();
            }

            if ((crashReportUpload == null || !crashReportUpload.Success)
                && (logUpload == null || !logUpload.Success))
            {
                _notificationService?.PopMessage(
                    crashReportUpload?.Error ?? logUpload?.Error
                    ?? LanguageManager.Instance.GameCrashReportModal_AiExportUploadFailedMessage.Current(),
                    LanguageManager.Instance.GameCrashReportModal_AiExportUploadFailedTitle.Current(),
                    GrowlLevel.Danger);
                return;
            }

            try
            {
                var template = await LoadAiAnalysisTemplateAsync();
                var markdown =
                    redactor.Apply(GenerateAiAnalysisMarkdown(template,
                                                               crashReportUpload,
                                                               logUpload,
                                                               crashReportContent,
                                                               logContent));

                using var exportProgress =
                    _notificationService?.PopProgress(
                        LanguageManager.Instance.GameCrashReportModal_AiExportWritingMessage.Current(),
                        LanguageManager.Instance.GameCrashReportModal_AiExportWritingTitle.Current());

                await using var stream = await file.OpenWriteAsync();
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteAsync(markdown);
            }
            catch
            {
                // NOTE: 上传已成功但后续写盘失败，回滚远端以免留下不可回收的公开日志。
                await RollbackUploadsAsync(uploaded);
                throw;
            }

            _notificationService?.PopMessage(
                LanguageManager.Instance.GameCrashReportModal_AiExportSuccessMessage.Current(),
                LanguageManager.Instance.GameCrashReportModal_AiExportSuccessTitle.Current(),
                GrowlLevel.Success,
                true);
        }
        catch (Exception ex)
        {
            _notificationService?.PopMessage(Program.IsDebug ? ex.ToString() : ex.Message,
                                             LanguageManager.Instance.GameCrashReportModal_AiExportFailedTitle.Current(),
                                             GrowlLevel.Danger);
        }
    }

    private async Task RollbackUploadsAsync(IEnumerable<CreateLogResponse> uploads)
    {
        if (_mclogsClient == null)
        {
            return;
        }

        foreach (var upload in uploads)
        {
            if (!upload.Success || string.IsNullOrEmpty(upload.Id) || string.IsNullOrEmpty(upload.Token))
            {
                continue;
            }

            try
            {
                await _mclogsClient.DeleteLogAsync(upload.Id, $"Bearer {upload.Token}");
            }
            catch
            {
                // 回滚失败不影响主流程：已尽力删除，剩余日志将在 mclo.gs TTL 后自动过期。
            }
        }
    }

    private async Task<string> LoadAiAnalysisTemplateAsync()
    {
        await using var stream = AssetLoader.Open(AiTemplateUri);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return await reader.ReadToEndAsync();
    }

    private async Task<CreateLogResponse?> UploadTextToMclogsAsync(string content,
                                                                  string contentKind,
                                                                  CancellationToken cancellationToken)
    {
        if (_mclogsClient == null)
        {
            return new(false, null, null, null, "mclo.gs client service is unavailable.");
        }

        try
        {
            return await _mclogsClient.CreateLogAsync(new(content,
                                                          $"{Program.Brand}/{Program.Version}",
                                                          [
                                                              new("content_kind", contentKind, "Content Kind", false),
                                                              new("instance_name",
                                                                  Report?.InstanceName,
                                                                  "Instance Name"),
                                                              new("instance_key",
                                                                  Report?.InstanceKey,
                                                                  "Instance Key",
                                                                  false),
                                                              new("minecraft_version",
                                                                  Report?.MinecraftVersion,
                                                                  "Minecraft Version"),
                                                              new("loader", Report?.LoaderLabel, "Loader"),
                                                              new("operating_system",
                                                                  Report?.OperatingSystem,
                                                                  "Operating System")
                                                          ]), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
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
        string? uploadedLogContent)
    {
        var markdown = template;
        foreach (var pair in
                 BuildAiTemplateTokens(crashReportUpload, logUpload, crashReportContent, uploadedLogContent))
        {
            markdown = markdown.Replace($"{{{{{pair.Key}}}}}", pair.Value, StringComparison.Ordinal);
        }

        return markdown;
    }

    private Dictionary<string, string> BuildAiTemplateTokens(
        CreateLogResponse? crashReportUpload,
        CreateLogResponse? logUpload,
        string? crashReportContent,
        string? uploadedLogContent)
    {
        var avaloniaVersion = typeof(Application).Assembly.GetName().Version?.ToString() ?? "Unknown";

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
            ["crash_report_raw_url"] = crashReportUpload?.Raw ?? crashReportUpload?.Url ?? "Unavailable",
            ["log_url"] = logUpload?.Url ?? "Unavailable",
            ["raw_log_url"] = logUpload?.Raw ?? logUpload?.Url ?? "Unavailable",
            ["log_file_path"] = EscapeMarkdownInline(Report?.LogFilePath),
            ["crash_report_path"] = EscapeMarkdownInline(Report?.CrashReportPath),
            ["command_line"] = EscapeCodeFenceContent(Report?.CommandLine),
            ["crash_report_excerpt"] = EscapeCodeFenceContent(GetPreviewCrashReport(crashReportContent)),
            ["last_log_lines"] = EscapeCodeFenceContent(GetPreviewLogLines(uploadedLogContent))
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
            Path.Combine(Report.GameDirectory, "logs", "latest.log")
        };

        foreach (var candidatePath in candidatePaths.Where(static x => !string.IsNullOrWhiteSpace(x)))
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
                // NOTE: 忽略该候选，尝试下一个。
            }
        }

        return string.IsNullOrWhiteSpace(Report.LastLogLines)
                   ? null
                   : BuildTailLogContent(Report
                                        .LastLogLines.Replace("\r\n", "\n", StringComparison.Ordinal)
                                        .Split('\n'));
    }

    private string? GetCrashReportContentForAiExport()
    {
        if (string.IsNullOrWhiteSpace(Report?.CrashReportPath) || !File.Exists(Report.CrashReportPath))
        {
            return null;
        }

        try
        {
            return BuildHeadContent(File.ReadLines(Report.CrashReportPath), MAX_CRASH_REPORT_UPLOAD_LINES);
        }
        catch
        {
            return null;
        }
    }

    private static string? BuildTailLogContent(IEnumerable<string> lines)
    {
        var tail = new Queue<string>(MAX_UPLOAD_LINES);
        foreach (var line in lines)
        {
            if (tail.Count == MAX_UPLOAD_LINES)
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
            if (Encoding.UTF8.GetByteCount(content) <= MAX_UPLOAD_BYTES)
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
            if (Encoding.UTF8.GetByteCount(builder.ToString()) + Encoding.UTF8.GetByteCount(candidateLine)
              > MAX_UPLOAD_BYTES)
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

        var lines = logContent.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').TakeLast(100);
        var preview = string.Join(Environment.NewLine, lines).Trim();
        return string.IsNullOrWhiteSpace(preview) ? "Unavailable" : preview;
    }

    private static string GetPreviewCrashReport(string? crashReportContent)
    {
        if (string.IsNullOrWhiteSpace(crashReportContent))
        {
            return "Unavailable";
        }

        var lines = crashReportContent.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Take(100);
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
