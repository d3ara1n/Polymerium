using System;
using System.Reactive.Subjects;
using Polymerium.App.Services.States;
using Polymerium.Trident;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Services;

public class InstanceStateAggregator : IDisposable
{
    #region Injected

    private readonly InstanceManager _instanceManager;

    #endregion

    public InstanceStateAggregator(InstanceManager instanceManager)
    {
        _instanceManager = instanceManager;

        instanceManager.InstanceInstalling += InstanceManagerOnInstanceInstalling;
        instanceManager.InstanceDeploying += InstanceManagerOnInstanceDeploying;
        instanceManager.InstanceUpdating += InstanceManagerOnInstanceUpdating;
    }

    public Subject<ITracklet> Stream { get; } = new();

    #region IDisposable Members

    public void Dispose()
    {
        Stream.Dispose();
        _instanceManager.InstanceDeploying -= InstanceManagerOnInstanceDeploying;
        _instanceManager.InstanceInstalling -= InstanceManagerOnInstanceInstalling;
        _instanceManager.InstanceUpdating -= InstanceManagerOnInstanceUpdating;
    }

    #endregion

    private void InstanceManagerOnInstanceUpdating(object? sender, UpdateTracker e)
    {
        e.StateUpdated += OnStateUpdated;
    }

    private void InstanceManagerOnInstanceDeploying(object? sender, DeployTracker e)
    {
        e.StateUpdated += OnStateUpdated;
    }

    private void InstanceManagerOnInstanceInstalling(object? sender, InstallTracker e)
    {
        e.StateUpdated += OnStateUpdated;
    }

    private void OnStateUpdated(TrackerBase tracker, TrackerState state)
    {
        switch (state)
        {
            case TrackerState.Idle:
                Stream.OnNext(new StateTracklet(tracker.Key,
                                                tracker switch
                                                {
                                                    UpdateTracker => InstanceState.Updating,
                                                    InstallTracker => InstanceState.Installing,
                                                    DeployTracker => InstanceState.Deploying,
                                                    _ => throw new NotImplementedException()
                                                }));
                break;
            case TrackerState.Running:
                // TODO
                break;
            case TrackerState.Faulted:
                Stream.OnNext(new ExceptionTracklet(tracker.Key,
                                                    tracker.FailureReason ?? new NotImplementedException()));
                tracker.StateUpdated -= OnStateUpdated;
                break;
            case TrackerState.Finished:
                Stream.OnNext(new StateTracklet(tracker.Key, InstanceState.Idle));
                tracker.StateUpdated -= OnStateUpdated;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}