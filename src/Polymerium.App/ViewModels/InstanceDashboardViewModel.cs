using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableCollections;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Core;
using Trident.Core.Engines.Launching;
using Trident.Core.Services;
using Trident.Core.Services.Instances;

namespace Polymerium.App.ViewModels;

public partial class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    PersistenceService persistenceService,
    ScrapService scrapService) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Private Fields

    private ISynchronizedView<ScrapModel, ScrapModel>? _liveView;
    private IDisposable? _liveSubscription;

    #endregion

    #region Statistics Properties

    [ObservableProperty]
    public partial int SessionCount { get; set; }

    [ObservableProperty]
    public partial int CrashCount { get; set; }

    #endregion

    #region Log Source Properties

    [ObservableProperty]
    public partial ObservableCollection<LogSourceModel> LogSources { get; set; } = [];

    [ObservableProperty]
    public partial LogSourceModel? SelectedLogSource { get; set; }

    #endregion

    #region Log Filter Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLogs))]
    public partial bool ShowInformation { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLogs))]
    public partial bool ShowWarning { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLogs))]
    public partial bool ShowError { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAutoScroll { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLogs))]
    [NotifyPropertyChangedFor(nameof(FilteredCount))]
    public partial string FilterText { get; set; } = string.Empty;

    #endregion

    #region Log Count Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial int ErrorCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial int WarningCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    public partial int InformationCount { get; set; }

    public int TotalCount => ErrorCount + WarningCount + InformationCount;

    public int FilteredCount => FilteredLogs?.Count ?? 0;

    #endregion

    #region Log Content Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(FilteredCount))]
    public partial NotifyCollectionChangedSynchronizedViewList<ScrapModel>? FilteredLogs { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ScrapModel>? FileLogs { get; set; }

    public bool IsEmpty => FilteredLogs == null || FilteredLogs.Count == 0;

    public string EmptyHint =>
        SelectedLogSource?.Kind == LogSourceKind.Live
            ? State == InstanceState.Running ? "正在等待日志..." : "游戏运行时将显示实时日志"
            : "选择日志文件以查看内容";

    #endregion

    #region Lifecycle

    protected override Task OnInitializeAsync()
    {
        // 加载统计数据
        SessionCount = persistenceService.GetSessionCount(Basic.Key);
        CrashCount = persistenceService.GetCrashCount(Basic.Key);

        // 初始化日志源列表
        RefreshLogSources();

        // 如果当前正在运行，绑定实时日志
        if (State == InstanceState.Running)
        {
            TryBindLiveLogs();
        }

        // 默认选择实时日志源
        SelectedLogSource = LogSources.FirstOrDefault();

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        UnbindLiveLogs();
        return Task.CompletedTask;
    }

    protected override void OnInstanceLaunching(LaunchTracker tracker)
    {
        base.OnInstanceLaunching(tracker);

        // 游戏启动时，如果当前选择的是实时日志，重新绑定
        if (SelectedLogSource?.Kind == LogSourceKind.Live)
        {
            // 延迟执行，确保 ScrapService 已创建 buffer
            BindLiveLogsWithRetryAsync();
        }

        OnPropertyChanged(nameof(EmptyHint));
    }

    protected override void OnInstanceLaunched(LaunchTracker tracker)
    {
        base.OnInstanceLaunched(tracker);

        // 游戏结束时刷新统计数据
        Dispatcher.UIThread.Post(() =>
        {
            SessionCount = persistenceService.GetSessionCount(Basic.Key);
            CrashCount = persistenceService.GetCrashCount(Basic.Key);
            RefreshLogSources();
            OnPropertyChanged(nameof(EmptyHint));
        });
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is nameof(ShowInformation) or nameof(ShowWarning) or nameof(ShowError) or nameof(FilterText))
        {
            ApplyFilter();
        }
    }

    partial void OnSelectedLogSourceChanged(LogSourceModel? value)
    {
        if (value == null)
        {
            UnbindLiveLogs();
            FilteredLogs = null;
            OnPropertyChanged(nameof(EmptyHint));
            return;
        }

        if (value.Kind == LogSourceKind.Live)
        {
            // 尝试绑定实时日志，如果 buffer 不存在则使用重试机制
            if (!TryBindLiveLogs())
            {
                // buffer 不存在，可能游戏刚启动，使用重试机制
                BindLiveLogsWithRetryAsync();
            }
        }
        else
        {
            UnbindLiveLogs();
            _ = LoadFileLogsAsync(value.FilePath!);
        }

        OnPropertyChanged(nameof(EmptyHint));
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void OpenLogsFolder()
    {
        var logsDir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "logs");
        if (Directory.Exists(logsDir))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(logsDir));
        }
    }

    [RelayCommand]
    private void OpenCrashReportsFolder()
    {
        var crashReportsDir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "crash-reports");
        if (Directory.Exists(crashReportsDir))
        {
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(crashReportsDir));
        }
    }

    [RelayCommand]
    private void RefreshLogSources()
    {
        var currentSelection = SelectedLogSource;
        LogSources.Clear();

        // 添加实时日志源
        LogSources.Add(LogSourceModel.CreateLive());

        // 扫描日志文件
        var logsDir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "logs");
        if (Directory.Exists(logsDir))
        {
            var logFiles = Directory
                          .GetFiles(logsDir, "*.log")
                          .Concat(Directory.GetFiles(logsDir, "*.txt"))
                          .OrderByDescending(File.GetLastWriteTime)
                          .Take(15);

            foreach (var file in logFiles)
            {
                LogSources.Add(LogSourceModel.CreateFile(file));
            }
        }

        // 恢复选择
        if (currentSelection != null)
        {
            SelectedLogSource =
                LogSources.FirstOrDefault(x => x.Kind == currentSelection.Kind
                                            && x.FilePath == currentSelection.FilePath)
             ?? LogSources.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void ClearFilter() => FilterText = string.Empty;

    #endregion

    #region Private Methods

    /// <summary>
    /// 尝试绑定实时日志，返回是否成功
    /// </summary>
    private bool TryBindLiveLogs()
    {
        UnbindLiveLogs();

        if (scrapService.TryGetBuffer(Basic.Key, out var buffer))
        {
            _liveView = buffer.CreateView(x => x);
            FilteredLogs = _liveView.ToNotifyCollectionChanged();

            // 计算各级别数量
            UpdateLogCounts(buffer);

            // 应用过滤
            ApplyFilter();

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(EmptyHint));
            return true;
        }

        FilteredLogs = null;
        ErrorCount = 0;
        WarningCount = 0;
        InformationCount = 0;
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyHint));
        return false;
    }

    /// <summary>
    /// 异步重试绑定实时日志
    /// </summary>
    private async void BindLiveLogsWithRetryAsync()
    {
        // 尝试绑定，最多重试10次，每次间隔200ms（共2秒）
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(200);

            // 检查是否仍然选择实时日志
            if (SelectedLogSource?.Kind != LogSourceKind.Live)
                return;

            if (scrapService.TryGetBuffer(Basic.Key, out _))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // 再次检查，避免竞态条件
                    if (SelectedLogSource?.Kind == LogSourceKind.Live)
                    {
                        TryBindLiveLogs();
                    }
                });
                return;
            }
        }
    }

    private void UnbindLiveLogs()
    {
        FilteredLogs?.Dispose();
        FilteredLogs = null;
        _liveView?.Dispose();
        _liveView = null;
        _liveSubscription?.Dispose();
        _liveSubscription = null;
    }

    private async Task LoadFileLogsAsync(string filePath)
    {
        try
        {
            var fileLogBuffer = new ObservableFixedSizeRingBuffer<ScrapModel>(10000);

            var lines = await File.ReadAllLinesAsync(filePath);
            var scraps = ParseLogLines(lines);

            foreach (var scrap in scraps)
            {
                fileLogBuffer.AddLast(scrap);
            }

            // 创建视图
            _liveView?.Dispose();
            _liveView = fileLogBuffer.CreateView(x => x);
            FilteredLogs = _liveView.ToNotifyCollectionChanged();

            // 计算各级别数量
            ErrorCount = fileLogBuffer.Count(x => x.Level == ScrapLevel.Error);
            WarningCount = fileLogBuffer.Count(x => x.Level == ScrapLevel.Warning);
            InformationCount = fileLogBuffer.Count(x => x.Level == ScrapLevel.Information);

            // 应用过滤
            ApplyFilter();

            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(TotalCount));
        }
        catch
        {
            FilteredLogs = null;
            ErrorCount = 0;
            WarningCount = 0;
            InformationCount = 0;
        }
    }

    private void UpdateLogCounts(ObservableFixedSizeRingBuffer<ScrapModel> buffer)
    {
        ErrorCount = buffer.Count(x => x.Level == ScrapLevel.Error);
        WarningCount = buffer.Count(x => x.Level == ScrapLevel.Warning);
        InformationCount = buffer.Count(x => x.Level == ScrapLevel.Information);
        OnPropertyChanged(nameof(TotalCount));
    }

    private void ApplyFilter()
    {
        _liveView?.AttachFilter(scrap =>
        {
            // 级别过滤
            var levelMatch = scrap.Level switch
            {
                ScrapLevel.Information => ShowInformation,
                ScrapLevel.Warning => ShowWarning,
                ScrapLevel.Error => ShowError,
                _ => true
            };

            if (!levelMatch)
                return false;

            // 内容过滤
            if (!string.IsNullOrEmpty(FilterText))
            {
                return scrap.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        });

        OnPropertyChanged(nameof(FilteredCount));
    }

    private static IEnumerable<ScrapModel> ParseLogLines(string[] lines)
    {
        // Minecraft 日志格式示例:
        // [12:00:00] [Render thread/INFO]: Loading...
        // [12:00:01] [Render thread/WARN]: Warning message
        // [12:00:02] [Server thread/ERROR]: Error message

        var regex = new Regex(@"^\[(\d{2}:\d{2}:\d{2})\]\s*\[([^/]+)/(\w+)\]:\s*(.*)$", RegexOptions.Compiled);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = regex.Match(line);
            if (match.Success)
            {
                var timeStr = match.Groups[1].Value;
                var thread = match.Groups[2].Value;
                var levelStr = match.Groups[3].Value;
                var message = match.Groups[4].Value;

                var level = levelStr.ToUpperInvariant() switch
                {
                    "ERROR" or "FATAL" or "SEVERE" => ScrapLevel.Error,
                    "WARN" or "WARNING" => ScrapLevel.Warning,
                    _ => ScrapLevel.Information
                };

                // 解析时间
                if (TimeSpan.TryParse(timeStr, out var time))
                {
                    var dateTime = DateTimeOffset.Now.Date.Add(time);
                    yield return new(message, level, dateTime, thread, levelStr);
                }
                else
                {
                    yield return new(message, level, DateTimeOffset.Now, thread, levelStr);
                }
            }
            else
            {
                // 无法解析的行作为普通信息日志
                yield return new(line, ScrapLevel.Information, DateTimeOffset.Now, "Unknown", "INFO");
            }
        }
    }

    #endregion
}
