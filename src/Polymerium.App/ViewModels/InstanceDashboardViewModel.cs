using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Trident.Abstractions;
using Trident.Abstractions.Tasks;
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using Trident.Core.Utilities;

namespace Polymerium.App.ViewModels;

public partial class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager,
    ScrapService scrapService,
    NotificationService notificationService
) : InstanceViewModelBase(bag, instanceManager, profileManager)
{
    #region Overrides

    protected override Task OnInitializeAsync()
    {
        InitializeLogSources();
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
    public partial LogSourceModelBase SelectedSource { get; set; }

    [ObservableProperty]
    public partial ICollection<ScrapModel>? LogCollection { get; set; }

    partial void OnSelectedSourceChanged(LogSourceModelBase value) => UpdateLogSource(value);

    [ObservableProperty]
    public partial bool IsOnAir { get; set; }

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

    private void UpdateLogSource(LogSourceModelBase source)
    {
        switch (source)
        {
            case LiveLogSourceModel:
                // 使用 ObservableCollections
                if (IsOnAir)
                {
                    // Attach
                    // Bind
                    if (scrapService.TryGetBuffer(Basic.Key, out var buffer))
                    {
                        LogCollection = buffer;
                    }
                }
                else
                {
                    LogCollection = null;
                }

                break;
            case FileLogSourceModel file:
                // 使用 DynamicData
                if (IsOnAir)
                {
                    // Detach
                    // 似乎不需要了，因为 ScrapService 会负责这个
                    // TODO: 此时文件 latest.log 和 debug.log 无法读取
                    LogCollection = null;
                }
                else
                {
                    if (File.Exists(file.Path))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(file.Path);
                            var container = new List<ScrapModel>();
                            ScrapModel? last = null;
                            foreach (var line in lines)
                            {
                                var item = ScrapHelper.Parse(line);
                                var appended = ScrapService.AppendToModel(item, last);
                                container.Add(appended);
                                last = appended;
                            }
                            LogCollection = container;
                        }
                        catch (Exception ex)
                        {
                            LogCollection = null;
                            notificationService.PopMessage(ex, "Read log file failed");
                        }
                    }
                    else
                    {
                        LogCollection = null;
                    }
                }

                // Set
                break;
        }
    }

    #endregion
}
