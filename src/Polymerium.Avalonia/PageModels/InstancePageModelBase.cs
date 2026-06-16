using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Widgets;
using TridentCore.Abstractions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core;
using TridentCore.Core.Services;
using Polymerium.Avalonia.Services;
using TridentCore.Core.Services.Instances;

namespace Polymerium.Avalonia.PageModels;

public abstract partial class InstancePageModelBase : ViewModelBase
{
    protected InstancePageModelBase(
        IViewContext<InstanceContextParameter> context,
        InstanceStateAggregator aggregator,
        InstanceManager instanceManager,
        ProfileManager profileManager
    )
    {
        _aggregator = aggregator;
        InstanceManager = instanceManager;
        ProfileManager = profileManager;
        if (context.Parameter is not null)
        {
            Basic = context.Parameter.Basic;
            Widgets = context.Parameter.Widgets;
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

    public record InstanceContextParameter(InstanceBasicModel Basic, WidgetBase[] Widgets);

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
    private readonly InstanceStateAggregator _aggregator;
    private IDisposable? _aggregatorSubscription;
    private TrackerBase? _currentTracker;

    #endregion

    #region Tracking

    public override Task InitializeAsync(CancellationToken cancellationToken)
    {
        ProfileManager.ProfileUpdated += OnProfileUpdated;

        _aggregatorSubscription = _aggregator.Watch(Basic.Key).Subscribe(snapshot =>
        {
            if (snapshot is null)
            {
                var completed = _currentTracker;
                _currentTracker = null;
                Dispatcher.UIThread.Post(() =>
                {
                    State = InstanceState.Idle;
                    switch (completed)
                    {
                        case UpdateTracker update:
                            OnInstanceUpdated(update);
                            break;
                        case DeployTracker deploy:
                            OnInstanceDeployed(deploy);
                            break;
                        case LaunchTracker launch:
                            OnInstanceLaunched(launch);
                            break;
                    }
                });
            }
            else
            {
                // 只在 tracker 变化时调 hook（避免每次 snapshot 更新重复调）
                if (!ReferenceEquals(snapshot.Tracker, _currentTracker))
                {
                    _currentTracker = snapshot.Tracker;
                    Dispatcher.UIThread.Post(() =>
                    {
                        State = snapshot.State;
                        switch (snapshot.Tracker)
                        {
                            case UpdateTracker update:
                                OnInstanceUpdating(update);
                                break;
                            case DeployTracker deploy:
                                OnInstanceDeploying(deploy);
                                break;
                            case LaunchTracker launch:
                                OnInstanceLaunching(launch);
                                break;
                        }
                    });
                }
                else
                {
                    Dispatcher.UIThread.Post(() => State = snapshot.State);
                }
            }
        });

        OnModelUpdated(Basic.Key, ProfileManager.GetImmutable(Basic.Key));

        return base.InitializeAsync(cancellationToken);
    }

    public override Task DeinitializeAsync()
    {
        ProfileManager.ProfileUpdated -= OnProfileUpdated;
        _aggregatorSubscription?.Dispose();
        return base.DeinitializeAsync();
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
