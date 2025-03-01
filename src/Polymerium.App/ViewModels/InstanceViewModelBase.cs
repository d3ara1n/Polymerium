using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Exceptions;
using Polymerium.App.Facilities;
using Polymerium.App.Models;
using Polymerium.App.Views;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.ViewModels;

public abstract partial class InstanceViewModelBase : ViewModelBase
{
    public InstanceViewModelBase(ViewBag bag, InstanceManager instanceManager, ProfileManager profileManager)
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

    protected virtual void OnInstanceUpdated(UpdateTracker tracker) { }
    protected virtual void OnInstanceDeployed(DeployTracker tracker) { }

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
        ProfileManager.ProfileUpdated += OnProfileUpdated;
        if (InstanceManager.IsTracking(Basic.Key, out var tracker))
            if (tracker is UpdateTracker update)
            {
                // 已经处于更新状态而未收到事件
                State = InstanceState.Updating;
                update.StateUpdated += OnProfileUpdateStateChanged;
            }

        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync(CancellationToken token)
    {
        InstanceManager.InstanceUpdating -= OnProfileUpdating;
        InstanceManager.InstanceDeploying -= OnProfileDeploying;
        ProfileManager.ProfileUpdated -= OnProfileUpdated;
        return Task.CompletedTask;
    }

    private void OnProfileUpdating(object? sender, UpdateTracker tracker)
    {
        Dispatcher.UIThread.Post(() => State = InstanceState.Updating);

        tracker.StateUpdated += OnProfileUpdateStateChanged;
        OnInstanceUpdating(tracker);
        // 更新的事情交给 ProfileManager.ProfileUpdated
    }

    private void OnProfileDeploying(object? sender, DeployTracker tracker)
    {
        Dispatcher.UIThread.Post(() => State = InstanceState.Deploying);

        tracker.StateUpdated += OnProfileDeployStateChanged;
        OnInstanceDeploying(tracker);
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

    private void OnProfileUpdated(object? sender, ProfileManager.ProfileChangedEventArgs e)
    {
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
    private InstanceBasicModel _basic;

    [ObservableProperty]
    private InstanceState _state = InstanceState.Idle;

    #endregion
}