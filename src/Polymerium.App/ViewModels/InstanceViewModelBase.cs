using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Widgets;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Tasks;
using Trident.Core;
using Trident.Core.Services;
using Trident.Core.Services.Instances;

namespace Polymerium.App.ViewModels;

public abstract partial class InstanceViewModelBase : ViewModelBase
{
    protected InstanceViewModelBase(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager)
    {
        InstanceManager = instanceManager;
        ProfileManager = profileManager;

        if (bag.Parameter is InstanceContextParameter context)
        {
            Basic = context.Basic;
            Widgets = context.Widgets;
        }
        else
        {
            throw new PageNotReachedException(GetType(), "Basic to the instance is not provided");
        }
    }

    #region Reactive

    [ObservableProperty]
    public partial InstanceState State { get; set; } = InstanceState.Idle;

    #endregion

    #region Nested type: InstanceContextParameter

    #region NestedType: InstanceContextParameter

    public record InstanceContextParameter(InstanceBasicModel Basic, WidgetBase[] Widgets);

    #endregion

    #endregion

    #region Protected

    protected virtual void OnModelUpdated(string key, Profile profile) { }

    protected virtual void OnInstanceUpdating(UpdateTracker tracker) { }

    protected virtual void OnInstanceDeploying(DeployTracker tracker) { }

    protected virtual void OnInstanceLaunching(LaunchTracker tracker) { }

    protected virtual void OnInstanceUpdated(UpdateTracker tracker) { }

    protected virtual void OnInstanceDeployed(DeployTracker tracker) { }

    protected virtual void OnInstanceLaunched(LaunchTracker tracker) { }

    #endregion

    #region Injected Protected

    protected readonly InstanceManager InstanceManager;
    protected readonly ProfileManager ProfileManager;

    #endregion

    #region Tracking

    protected override Task OnInitializeAsync()
    {
        InstanceManager.InstanceUpdating += OnInstanceUpdating;
        InstanceManager.InstanceDeploying += OnInstanceDeploying;
        InstanceManager.InstanceLaunching += OnInstanceLaunching;
        ProfileManager.ProfileUpdated += OnProfileUpdated;
        if (InstanceManager.IsTracking(Basic.Key, out var tracker))
        {
            switch (tracker)
            {
                case UpdateTracker update:
                    // 已经处于更新状态而未收到事件
                    State = InstanceState.Updating;
                    update.StateUpdated += OnInstanceUpdateStateChanged;
                    OnInstanceUpdating(update);
                    break;
                case DeployTracker deploy:
                    // 已经处于部署状态而未收到事件
                    State = InstanceState.Deploying;
                    deploy.StateUpdated += OnInstanceDeployStateChanged;
                    OnInstanceDeploying(deploy);
                    break;
                case LaunchTracker launch:
                    // 已经处于启动状态而未收到事件
                    State = InstanceState.Running;
                    launch.StateUpdated += OnInstanceLaunchingStateChanged;
                    OnInstanceLaunching(launch);
                    break;
            }
        }

        OnModelUpdated(Basic.Key, ProfileManager.GetImmutable(Basic.Key));

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        InstanceManager.InstanceUpdating -= OnInstanceUpdating;
        InstanceManager.InstanceDeploying -= OnInstanceDeploying;
        InstanceManager.InstanceLaunching -= OnInstanceLaunching;
        ProfileManager.ProfileUpdated -= OnProfileUpdated;
        return Task.CompletedTask;
    }

    private void OnInstanceUpdating(object? sender, UpdateTracker tracker)
    {
        if (tracker.Key != Basic.Key)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => State = InstanceState.Updating);

        tracker.StateUpdated += OnInstanceUpdateStateChanged;
        OnInstanceUpdating(tracker);
        // 更新的事情交给 ProfileManager.ProfileUpdated
    }

    private void OnInstanceDeploying(object? sender, DeployTracker tracker)
    {
        if (tracker.Key != Basic.Key)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => State = InstanceState.Deploying);

        tracker.StateUpdated += OnInstanceDeployStateChanged;
        OnInstanceDeploying(tracker);
    }

    private void OnInstanceLaunching(object? sender, LaunchTracker tracker)
    {
        if (tracker.Key != Basic.Key)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => State = InstanceState.Running);

        tracker.StateUpdated += OnInstanceLaunchingStateChanged;
        OnInstanceLaunching(tracker);
    }

    private void OnInstanceUpdateStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnInstanceUpdateStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
                OnInstanceUpdated((UpdateTracker)sender);
            });
        }
    }

    private void OnInstanceDeployStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnInstanceDeployStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
                OnInstanceDeployed((DeployTracker)sender);
            });
        }
    }

    private void OnInstanceLaunchingStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnInstanceLaunchingStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
                OnInstanceLaunched((LaunchTracker)sender);
            });
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        if (e.Key != Basic.Key)
        {
            return;
        }

        OnModelUpdated(e.Key, e.Value);
    }

    #endregion

    #region Direct

    public InstanceBasicModel Basic { get; }

    public WidgetBase[] Widgets { get; }

    #endregion
}
