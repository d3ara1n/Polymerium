using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Mvvm.Activation;
using ObservableCollections;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Engines.Launching;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class InstanceDashboardPageModel(
    IViewContext<InstancePageModelBase.InstanceContextParameter> context,
    InstanceStateAggregator aggregator,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    ScrapService scrapService,
    NotificationService notificationService,
    PersistenceService persistenceService) : InstancePageModelBase(context, aggregator, instanceManager, profileManager)
{
    #region Instance State

    protected override void OnInstanceLaunching(LaunchTracker tracker)
    {
        CallCleanup();

        if (tracker.Process is { } process)
        {
            StartMonitoring(process);
            Dispatcher.UIThread.Post(() => MemoryAssigned = tracker.Options.MaxMemory);
        }
        else
        {
            tracker.ProcessAssigned += OnProcessAssigned;
        }

        tracker.StateUpdated += OnStateUpdated;

        _callbackCleanup = () =>
        {
            tracker.ProcessAssigned -= OnProcessAssigned;
            tracker.StateUpdated -= OnStateUpdated;
            StopMonitoring();
        };

        IsOnAir = true;
        Dispatcher.UIThread.Post(() => UpdateLogSource(SelectedSource));
        return;

        void OnProcessAssigned(object? sender, Process got)
        {
            tracker.ProcessAssigned -= OnProcessAssigned;
            StartMonitoring(got);
            Dispatcher.UIThread.Post(() => MemoryAssigned = tracker.Options.MaxMemory);
        }

        void OnStateUpdated(TrackerBase _, TrackerState state)
        {
            if (state is TrackerState.Faulted or TrackerState.Finished)
            {
                CallCleanup();
                IsOnAir = false;
                StopMonitoring();
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateLogSource(SelectedSource);
                    SessionCount++;
                    if (state is TrackerState.Faulted)
                    {
                        CrashCount++;
                    }
                });
            }
        }
    }

    #endregion

    #region Reactive

    public ObservableCollection<LogSourceModelBase> Sources { get; } = [];

    [ObservableProperty]
    public partial LogSourceModelBase? SelectedSource { get; set; }

    partial void OnSelectedSourceChanged(LogSourceModelBase? value) => UpdateLogSource(value);

    [ObservableProperty]
    public partial string FilterText { get; set; } = string.Empty;

    partial void OnFilterTextChanged(string value) => SetupView();

    [ObservableProperty]
    public partial bool IsFilterInformation { get; set; } = true;

    partial void OnIsFilterInformationChanged(bool value) => SetupView();

    [ObservableProperty]
    public partial bool IsFilterWarning { get; set; } = true;

    partial void OnIsFilterWarningChanged(bool value) => SetupView();

    [ObservableProperty]
    public partial bool IsFilterError { get; set; } = true;

    partial void OnIsFilterErrorChanged(bool value) => SetupView();

    [ObservableProperty]
    public partial IList<ScrapModel>? LogCollection { get; set; }

    [ObservableProperty]
    public partial NotifyCollectionChangedSynchronizedViewList<ScrapModel>? FilteredLogCollection { get; set; }

    [ObservableProperty]
    public partial bool IsOnAir { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuccessRate))]
    public partial int SessionCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuccessRate))]
    public partial int CrashCount { get; set; }

    public double SuccessRate => SessionCount > 0 ? (double)(SessionCount - CrashCount) / SessionCount * 100 : 100.0;

    [ObservableProperty]
    public partial double CpuPercent { get; set; }

    [ObservableProperty]
    public partial uint MemoryUsage { get; set; }

    [ObservableProperty]
    public partial uint MemoryAssigned { get; set; }

    [ObservableProperty]
    public partial TimeSpan Uptime { get; set; }

    #endregion

    #region Fields

    private ISynchronizedView<ScrapModel, ScrapModel>? _collectionView;
    private bool _isDisposed;

    private CancellationTokenSource? _monitoringTokenSource;

    private Action? _callbackCleanup;

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        InitializeLogSources();
        SelectedSource = Sources?.FirstOrDefault();

        SessionCount = persistenceService.GetSessionCount(Basic.Key);
        CrashCount = persistenceService.GetCrashCount(Basic.Key);

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _isDisposed = true;
        CallCleanup();
        FilteredLogCollection?.Dispose();
        FilteredLogCollection = null;
        _collectionView?.Dispose();
        _collectionView = null;
        return Task.CompletedTask;
    }

    #endregion

    #region Other: Logs

    private void InitializeLogSources()
    {
        // NOTE: Live 源内容不变，只需添加一次；不关心具体目录，日志最终都会落在 build/logs。
        Sources.Clear();
        var live = new LiveLogSourceModel();
        Sources.Add(live);
        var dir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "logs");
        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.log", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                Sources.Add(new FileLogSourceModel { Path = file });
            }
        }
    }

    private void UpdateLogSource(LogSourceModelBase? source)
    {
        if (_isDisposed)
        {
            return;
        }
        if (!IsOnAir)
        {
            // NOTE: 运行时不清空——LogCollection 是外部共享集合。
            LogCollection?.Clear();
        }

        _collectionView?.Dispose();
        FilteredLogCollection?.Dispose();

        switch (source)
        {
            case LiveLogSourceModel:
                if (IsOnAir)
                {
                    if (scrapService.TryGetBuffer(Basic.Key, out var buffer))
                    {
                        _collectionView = buffer.CreateView(x => x);
                        SetupView(_collectionView);
                        LogCollection = buffer;
                        FilteredLogCollection = _collectionView.ToNotifyCollectionChanged();
                    }
                }

                break;
            case FileLogSourceModel file:
                if (!IsOnAir)
                {
                    if (File.Exists(file.Path))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(file.Path);
                            var container = new ObservableList<ScrapModel>(lines.Length);
                            ScrapModel? last = null;
                            foreach (var line in lines)
                            {
                                var item = ScrapHelper.Parse(line);
                                var appended = ScrapService.AppendToModel(item, last);
                                container.Add(appended);
                                last = appended;
                            }

                            _collectionView = container.CreateView(x => x);
                            SetupView(_collectionView);
                            LogCollection = container;
                            FilteredLogCollection = _collectionView.ToNotifyCollectionChanged();
                        }
                        catch (Exception ex)
                        {
                            notificationService.PopMessage(ex,
                                                           LanguageManager.Instance.InstanceDashboardPage_ReadLogDangerNotificationTitle.Current());
                        }
                    }
                }

                break;
        }
    }

    private void SetupView()
    {
        if (_collectionView != null)
        {
            SetupView(_collectionView);
        }
    }

    private void SetupView(ISynchronizedView<ScrapModel, ScrapModel> view)
    {
        var predicate = BuildFilter();
        if (predicate != null)
        {
            view.AttachFilter(predicate);
        }
        else
        {
            view.ResetFilter();
        }
    }

    private Func<ScrapModel, bool>? BuildFilter()
    {
        // NOTE: 三个级别全开且无搜索文本时不过滤（null = 全放行）。
        var allLevels = IsFilterError && IsFilterWarning && IsFilterInformation;
        var hasSearch = !string.IsNullOrWhiteSpace(FilterText);

        if (allLevels && !hasSearch)
        {
            return null;
        }

        return item =>
        {
            if (!IsFilterError && item.Level == ScrapLevel.Error)
            {
                return false;
            }

            if (!IsFilterWarning && item.Level == ScrapLevel.Warning)
            {
                return false;
            }

            if (!IsFilterInformation && item.Level == ScrapLevel.Information)
            {
                return false;
            }

            if (hasSearch)
            {
                var keyword = FilterText!;
                return (item.Message?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Thread?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (item.Sender?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false);
            }

            return true;
        };
    }

    #endregion

    #region Other: Metrics

    private void CallCleanup()
    {
        _callbackCleanup?.Invoke();
        _callbackCleanup = null;
    }

    private void StartMonitoring(Process process)
    {
        StopMonitoring();

        _monitoringTokenSource = new();
        _ = MonitorAsync(process, _monitoringTokenSource.Token);
    }

    private void StopMonitoring()
    {
        if (_monitoringTokenSource is { } cts)
        {
            _monitoringTokenSource = null;
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }

            cts.Dispose();
        }
    }

    private async Task MonitorAsync(Process process, CancellationToken token)
    {
        process.Refresh();
        var errorCount = 0;
        TimeSpan? lastCpuTime = null;
        var lastSampleTime = DateTime.Now;
        var cpuCount = Environment.ProcessorCount;
        while (!token.IsCancellationRequested)
        {
            try
            {
                (lastSampleTime, lastCpuTime) = await MonitorCoreAsync(process, lastSampleTime, lastCpuTime, cpuCount);

                if (process.HasExited)
                {
                    StopMonitoring();
                }
            }
            catch (InvalidOperationException)
            {
                // NOTE: 进程未启动（InvalidOperationException）给三次宽限再停止监控。
                errorCount++;
                if (errorCount > 3)
                {
                    StopMonitoring();
                }
            }
            catch (Exception)
            {
                StopMonitoring();
            }
            finally
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // NOTE: 正常退出（token 取消），不算错误。
                }
            }
        }
    }

    private async Task<(DateTime, TimeSpan)> MonitorCoreAsync(
        Process process,
        DateTime lastSampleTime,
        TimeSpan? lastCpuTime,
        int cpuCount)
    {
        if (process.HasExited)
        {
            return (DateTime.Now, TimeSpan.Zero);
        }

        process.Refresh();

        // NOTE: 进程未启动时这行抛 InvalidOperationException，被外围捕获并进入失败倒计时。
        var cpuTime = process.TotalProcessorTime;
        var sampleTime = DateTime.Now;

        var cpuPercent = 0.0d;
        var memory = process.WorkingSet64 / 1024 / 1024;
        var uptime = sampleTime - process.StartTime;

        if (lastCpuTime.HasValue)
        {
            var cpuDelta = (cpuTime - lastCpuTime.Value).TotalMilliseconds;
            var elapsed = (sampleTime - lastSampleTime).TotalMilliseconds;
            cpuPercent = elapsed > 0 ? cpuDelta / (elapsed * cpuCount) * 100.0 : 0.0d;
        }

        Dispatcher.UIThread.Post(() =>
        {
            CpuPercent = cpuPercent;
            MemoryUsage = (uint)memory;
            Uptime = uptime;
        });

        return (sampleTime, cpuTime);
    }

    #endregion

    #region Commands

    [RelayCommand]
    private Task OpenLogsDirectory()
    {
        var dir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "logs");
        if (Directory.Exists(dir))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
                                                           new(dir),
                                                           LanguageManager.Instance.InstanceDashboardPage_OpenLogsFolderDangerNotificationTitle.Current(),
                                                           notificationService);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task OpenCrashReportsDirectory()
    {
        var dir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "crash-reports");
        if (Directory.Exists(dir))
        {
            return TopLevelHelper.LaunchDirectoryInfoAsync(TopLevelHelper.GetTopLevel(),
                                                           new(dir),
                                                           LanguageManager.Instance.InstanceDashboardPage_OpenCrashReportsFolderDangerNotificationTitle.Current(),
                                                           notificationService);
        }

        return Task.CompletedTask;
    }

    #endregion
}
