using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Lucide;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Properties;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Widgets;
using Trident.Core;
using Trident.Core.Services;
using Trident.Core.Services.Instances;
using Trident.Abstractions;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.ViewModels;

public partial class InstanceViewModel : ViewModelBase
{
    public InstanceViewModel(
        ViewBag bag,
        ProfileManager profileManager,
        InstanceManager instanceManager,
        WidgetHostService widgetHostService)
    {
        _profileManager = profileManager;
        _instanceManager = instanceManager;
        SelectedPage = bag.Parameter switch
                       {
                           CompositeParameter it => PageEntries.FirstOrDefault(x => x.Page == it.Subview),
                           _ => null
                       }
                    ?? PageEntries.FirstOrDefault();

        var key = bag.Parameter switch
        {
            CompositeParameter p => p.Key,
            string s => s,
            _ => throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided")
        };
        if (profileManager.TryGetImmutable(key, out var profile))
        {
            Basic = new(key, profile.Name, profile.Setup.Version, profile.Setup.Loader, profile.Setup.Source);
            Context = new(Basic,
            [
                .. widgetHostService.WidgetTypes.Select(type =>
                {
                    var widget = (WidgetBase)Activator.CreateInstance(type)!;
                    widget.Context = widgetHostService.GetOrCreateContext(Basic.Key, type.Name);
                    return widget;
                })
            ]);
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView),
                                              Resources.InstanceView_KeyNotFoundExceptionMessage
                                                       .Replace("{0}", key));
        }
    }

    #region Commands

    [RelayCommand]
    private void OpenFolder()
    {
        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        TopLevel.GetTopLevel(MainWindow.Instance)?.Launcher.LaunchDirectoryInfoAsync(new(dir));
    }

    #endregion

    #region Nested type: CompositeParameter

    public record CompositeParameter(string Key, Type Subview);

    #endregion

    #region Direct

    public InstanceBasicModel Basic { get; }
    public InstanceViewModelBase.InstanceContextParameter Context { get; }

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        // 终究还是得有个 InstanceStateAggregator
        _instanceManager.InstanceUpdating += OnProfileUpdating;
        _instanceManager.InstanceDeploying += OnProfileDeploying;
        _instanceManager.InstanceLaunching += OnProfileLaunching;
        _profileManager.ProfileUpdated += OnProfileUpdated;
        if (_instanceManager.IsTracking(Basic.Key, out var tracker))
        {
            switch (tracker)
            {
                case UpdateTracker update:
                    // 已经处于更新状态而未收到事件
                    State = InstanceState.Updating;
                    update.StateUpdated += OnProfileUpdateStateChanged;
                    break;
                case DeployTracker deploy:
                    // 已经处于部署状态而未收到事件
                    State = InstanceState.Deploying;
                    deploy.StateUpdated += OnProfileDeployStateChanged;
                    break;
                case LaunchTracker launch:
                    // 已经处于启动状态而未收到事件
                    State = InstanceState.Running;
                    launch.StateUpdated += OnProfileLaunchingStateChanged;
                    break;
            }
        }

        foreach (var widget in Context.Widgets)
        {
            await widget.InitializeAsync();
        }
    }

    protected override async Task OnDeinitializeAsync(CancellationToken token)

    {
        _instanceManager.InstanceUpdating -= OnProfileUpdating;
        _instanceManager.InstanceDeploying -= OnProfileDeploying;
        _instanceManager.InstanceLaunching -= OnProfileLaunching;
        _profileManager.ProfileUpdated -= OnProfileUpdated;

        foreach (var widget in Context.Widgets)
        {
            await widget.DeinitializeAsync();
        }
    }

    #endregion

    #region Injected

    private readonly InstanceManager _instanceManager;
    private readonly ProfileManager _profileManager;

    #endregion

    #region Tracking

    private void OnProfileUpdating(object? sender, UpdateTracker tracker)
    {
        if (tracker.Key != Basic.Key)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => State = InstanceState.Updating);

        tracker.StateUpdated += OnProfileUpdateStateChanged;
        // 更新的事情交给 ProfileManager.ProfileUpdated
    }

    private void OnProfileDeploying(object? sender, DeployTracker tracker)
    {
        if (tracker.Key != Basic.Key)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => State = InstanceState.Deploying);

        tracker.StateUpdated += OnProfileDeployStateChanged;
    }

    private void OnProfileLaunching(object? sender, LaunchTracker tracker)
    {
        if (tracker.Key != Basic.Key)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => State = InstanceState.Running);

        tracker.StateUpdated += OnProfileLaunchingStateChanged;
    }

    private void OnProfileUpdateStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnProfileUpdateStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
            });
        }
    }

    private void OnProfileDeployStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnProfileDeployStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
            });
        }
    }

    private void OnProfileLaunchingStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnProfileLaunchingStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
            });
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        if (e.Key != Basic.Key)
        {
            return;
        }

        Basic.Name = e.Value.Name;
        Basic.Version = e.Value.Setup.Version;
        Basic.Loader = e.Value.Setup.Loader;
        Basic.Source = e.Value.Setup.Source;
        Basic.UpdateIcon();
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial ObservableCollection<InstanceSubpageEntryModel> PageEntries { get; set; } =
    [
        // Home
        new(typeof(InstanceHomeView), PackIconLucideKind.LayoutDashboard),
        // Setup
        new(typeof(InstanceSetupView), PackIconLucideKind.Boxes),
        // Assets
        new(typeof(InstanceAssetsView), PackIconLucideKind.CassetteTape),
        // Widgets
        new(typeof(InstanceWidgetsView), PackIconLucideKind.Blocks),
        // Statistics
        new(typeof(InstanceActivitiesView), PackIconLucideKind.ChartNoAxesCombined),
        // Storage
        new(typeof(InstanceStorageView), PackIconLucideKind.ChartPie),
        // Properties
        new(typeof(InstancePropertiesView), PackIconLucideKind.Wrench)
    ];

    [ObservableProperty]
    public partial InstanceSubpageEntryModel? SelectedPage { get; set; }

    [ObservableProperty]
    public partial InstanceState State { get; set; } = InstanceState.Idle;

    #endregion
}
