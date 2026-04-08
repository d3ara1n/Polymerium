using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableCollections;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Abstractions.Tasks;
using Trident.Core.Engines.Launching;
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    ScrapService scrapService,
    NotificationService notificationService,
    PersistenceService persistenceService
) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Overrides

    protected override Task OnInitializeAsync()
    {
        InitializeLogSources();
        SelectedSource = Sources?.FirstOrDefault();

        SessionCount = persistenceService.GetSessionCount(Basic.Key);
        CrashCount = persistenceService.GetCrashCount(Basic.Key);

        return Task.CompletedTask;
    }

    #endregion

    #region State

    protected override void OnInstanceLaunching(LaunchTracker tracker)
    {
        tracker.StateUpdated += OnStateUpdated;
        IsOnAir = true;
        Dispatcher.UIThread.Post(() => UpdateLogSource(SelectedSource));

        return;

        void OnStateUpdated(TrackerBase _, TrackerState state)
        {
            if (state is TrackerState.Faulted or TrackerState.Finished)
            {
                tracker.StateUpdated -= OnStateUpdated;
                IsOnAir = false;
                Dispatcher.UIThread.Post(() => UpdateLogSource(SelectedSource));
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

    public double SuccessRate =>
        SessionCount > 0 ? (double)(SessionCount - CrashCount) / SessionCount * 100 : 100.0;

    #endregion

    #region Direct

    private ISynchronizedView<ScrapModel, ScrapModel>? _collectionView;

    [RelayCommand]
    private void OpenLogsDirectory()
    {
        var dir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "logs");
        if (Directory.Exists(dir))
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
    }

    [RelayCommand]
    private void OpenCrashReportsDirectory()
    {
        var dir = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "crash-reports");
        if (Directory.Exists(dir))
            TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
    }

    #endregion

    #region Other

    private void InitializeLogSources()
    {
        // 由于内容是不变的，所以只需要添加一次
        // 这里不在乎是否是哪个实际目录，因为最终都会出现在 build/logs 中
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
        if (!IsOnAir)
        {
            // 避免清空，因为运行中的 Collection 是共享的外部引入的集合
            LogCollection?.Clear();
        }
        _collectionView?.Dispose();
        FilteredLogCollection?.Dispose();

        switch (source)
        {
            case LiveLogSourceModel:
                if (IsOnAir)
                {
                    // Attach
                    // Bind
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
                            notificationService.PopMessage(ex, "Read log file failed");
                        }
                    }
                }

                // Set
                break;
        }
    }

    private void SetupView()
    {
        if (_collectionView != null)
            SetupView(_collectionView);
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
        // 如果三个级别全部开启且没有搜索文本 → 无过滤
        var allLevels = IsFilterError && IsFilterWarning && IsFilterInformation;
        var hasSearch = !string.IsNullOrWhiteSpace(FilterText);

        if (allLevels && !hasSearch)
            return null;

        return item =>
        {
            if (!IsFilterError && item.Level == ScrapLevel.Error)
                return false;
            if (!IsFilterWarning && item.Level == ScrapLevel.Warning)
                return false;
            if (!IsFilterInformation && item.Level == ScrapLevel.Information)
                return false;

            if (hasSearch)
            {
                var keyword = FilterText!;
                return (
                        item.Message?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false
                    )
                    || (item.Thread?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (
                        item.Sender?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false
                    );
            }

            return true;
        };
    }

    #endregion
}
