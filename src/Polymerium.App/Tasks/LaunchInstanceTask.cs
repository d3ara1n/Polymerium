using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Tasks;

public class LaunchInstanceTask : TaskBase
{
    private readonly LaunchTracker _tracker;

    public LaunchInstanceTask(LaunchTracker tracker) : base(tracker.Key, $"Launching {tracker.Key}", "Preparing...")
    {
        _tracker = tracker;
        tracker.StateUpdated += Tracker_StateUpdated;
        tracker.Fired += Tracker_Fired;
    }

    private void Tracker_StateUpdated(TrackerBase sender, TaskState state) =>
        UpdateProgress(state, failure: sender.FailureReason);

    private void Tracker_Fired(LaunchTracker sender) => UpdateProgress(TaskState.Running,
        stage: $"Running {_tracker.Key}", status: "Waiting process to exit...");

    protected override void OnAbort() => base.OnAbort();
}