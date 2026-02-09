using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Trident.Abstractions;
using Trident.Abstractions.Tasks;
using Trident.Core.Services;
using Trident.Core.Services.Instances;

namespace Polymerium.App.ViewModels;

public partial class InstanceDashboardViewModel(
    ViewBag bag,
    InstanceManager instanceManager,
    ProfileManager profileManager) : InstanceViewModelBase(bag, instanceManager, profileManager)
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
        if (SelectedSource is LiveLogSourceModel)
        {
            // Attach
            // Bind
        }

        return;

        void OnStateUpdated(TrackerBase _, TrackerState state)
        {
            if (state is TrackerState.Faulted or TrackerState.Finished)
            {
                tracker.StateUpdated -= OnStateUpdated;
                if (SelectedSource is LiveLogSourceModel)
                {
                    // Detach
                    // Leave Behind
                }

                IsOnAir = false;
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
        var files = new[] { "latest.log", "debug.log" }
                   .Select(x => new FileLogSourceModel
                    {
                        Path = Path.Combine(PathDef.Default.DirectoryOfBuild(Basic.Key), "logs", x)
                    })
                   .ToList();
        var live = new LiveLogSourceModel();
        Sources.Add(live);
        foreach (var item in files)
        {
            Sources.Add(item);
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
                }

                break;
            case FileLogSourceModel:
                // 使用 DynamicData
                if (IsOnAir)
                {
                    // Detach
                }

                // Set
                break;
        }
    }

    #endregion
}
