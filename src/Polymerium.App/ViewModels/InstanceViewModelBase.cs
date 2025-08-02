using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.App.Widgets;
using Polymerium.Trident;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.ViewModels;

public abstract partial class InstanceViewModelBase : ViewModelBase
{
    protected InstanceViewModelBase(
        ViewBag bag,
        InstanceManager instanceManager,
        ProfileManager profileManager)
    {
        InstanceManager = instanceManager;
        ProfileManager = profileManager;

        if (bag.Parameter is string key)
        {
            if (profileManager.TryGetImmutable(key, out var profile))
            {
                UpdateBasic(key, profile);
                OnUpdateModel(key, profile);
            }
            else
            {
                throw new PageNotReachedException(typeof(InstanceView),
                                                  $"Key '{key}' is not valid instance or not found");
            }
        }
        else
        {
            throw new PageNotReachedException(typeof(InstanceView), "Key to the instance is not provided");
        }
    }

    #region Protected

    protected virtual void OnUpdateModel(string key, Profile profile) { }

    protected virtual void OnInstanceUpdating(UpdateTracker tracker) { }

    protected virtual void OnInstanceDeploying(DeployTracker tracker) { }
    protected virtual void OnInstanceLaunching(LaunchTracker tracker) { }

    protected virtual void OnInstanceUpdated(UpdateTracker tracker) { }
    protected virtual void OnInstanceDeployed(DeployTracker tracker) { }
    protected virtual void OnInstanceLaunched(LaunchTracker tracker) { }

    #endregion

    #region Injected

    protected readonly InstanceManager InstanceManager;
    protected readonly ProfileManager ProfileManager;

    #endregion

    #region Tracking

    protected override Task OnInitializedAsync(CancellationToken token)
    {
        InstanceManager.InstanceUpdating += OnProfileUpdating;
        InstanceManager.InstanceDeploying += OnProfileDeploying;
        InstanceManager.InstanceLaunching += OnProfileLaunching;
        ProfileManager.ProfileUpdated += OnProfileUpdated;
        if (InstanceManager.IsTracking(Basic.Key, out var tracker))
            if (tracker is UpdateTracker update)
            {
                // 已经处于更新状态而未收到事件
                State = InstanceState.Updating;
                update.StateUpdated += OnProfileUpdateStateChanged;
                OnInstanceUpdating(update);
            }
            else if (tracker is DeployTracker deploy)
            {
                // 已经处于部署状态而未收到事件
                State = InstanceState.Deploying;
                deploy.StateUpdated += OnProfileDeployStateChanged;
                OnInstanceDeploying(deploy);
            }
            else if (tracker is LaunchTracker launch)
            {
                // 已经处于启动状态而未收到事件
                State = InstanceState.Running;
                launch.StateUpdated += OnProfileLaunchingStateChanged;
                OnInstanceLaunching(launch);
            }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        InstanceManager.InstanceUpdating -= OnProfileUpdating;
        InstanceManager.InstanceDeploying -= OnProfileDeploying;
        InstanceManager.InstanceLaunching -= OnProfileLaunching;
        ProfileManager.ProfileUpdated -= OnProfileUpdated;
        return Task.CompletedTask;
    }

    private void OnProfileUpdating(object? sender, UpdateTracker tracker)
    {
        if (tracker.Key != Basic.Key)
            return;

        Dispatcher.UIThread.Post(() => State = InstanceState.Updating);

        tracker.StateUpdated += OnProfileUpdateStateChanged;
        OnInstanceUpdating(tracker);
        // 更新的事情交给 ProfileManager.ProfileUpdated
    }

    private void OnProfileDeploying(object? sender, DeployTracker tracker)
    {
        if (tracker.Key != Basic.Key)
            return;

        Dispatcher.UIThread.Post(() => State = InstanceState.Deploying);

        tracker.StateUpdated += OnProfileDeployStateChanged;
        OnInstanceDeploying(tracker);
    }

    private void OnProfileLaunching(object? sender, LaunchTracker tracker)
    {
        if (tracker.Key != Basic.Key)
            return;

        Dispatcher.UIThread.Post(() => State = InstanceState.Running);

        tracker.StateUpdated += OnProfileLaunchingStateChanged;
        OnInstanceLaunching(tracker);
    }

    private void OnProfileUpdateStateChanged(TrackerBase sender, TrackerState state)
    {
        if (state is TrackerState.Faulted or TrackerState.Finished)
        {
            sender.StateUpdated -= OnProfileUpdateStateChanged;
            Dispatcher.UIThread.Post(() =>
            {
                State = InstanceState.Idle;
                OnInstanceUpdated((UpdateTracker)sender);
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
                OnInstanceDeployed((DeployTracker)sender);
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
                OnInstanceLaunched((LaunchTracker)sender);
            });
        }
    }

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
        if (e.Key != Basic.Key)
            return;

        UpdateBasic(e.Key, e.Value);
        OnUpdateModel(e.Key, e.Value);
    }


    private void UpdateBasic(string key, Profile profile)
    {
        Basic = new InstanceBasicModel(key,
                                       profile.Name,
                                       profile.Setup.Version,
                                       profile.Setup.Loader,
                                       profile.Setup.Source);
    }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial InstanceBasicModel Basic { get; set; }

    [ObservableProperty]
    public partial InstanceState State { get; set; } = InstanceState.Idle;

    #endregion
}