using System;
using Polymerium.Trident.Services.Instances;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Tasks;

public class DeployInstanceTask : TaskBase
{
    private readonly TrackerBase _tracker;

    public DeployInstanceTask(DeployTracker tracker) : base(tracker.Key, $"Deploying {tracker.Key}", "Preparing...")
    {
        _tracker = tracker;
        tracker.StageUpdated += Tracker_StageUpdated;
        tracker.StateUpdated += Tracker_StateUpdated;
        tracker.FileSolidified += Tracker_FileSolidified;
    }

    private void Tracker_StageUpdated(DeployTracker sender, DeployStage stage)
    {
        ReportProgress(status: stage switch
        {
            DeployStage.CheckArtifact => "Checking artifact...",
            DeployStage.InstallVanilla => "Installing vanilla...",
            DeployStage.ProcessLoaders => "Processing loaders...",
            DeployStage.ResolveAttachments => "Resolving attachments...",
            DeployStage.BuildArtifact => "Building artifact...",
            DeployStage.BuildTransient => "Building transient...",
            DeployStage.SolidifyTransient => "Solidifying transient...",
            _ => throw new NotImplementedException()
        });
    }


    private void Tracker_StateUpdated(TrackerBase sender, TaskState state)
    {
        if (state == TaskState.Faulted) FailureReason = sender.FailureReason;
        UpdateProgress(state);
    }

    private void Tracker_FileSolidified(DeployTracker sender, uint finished, uint total)
    {
        ReportProgress(finished == total ? 100 : finished * 100 / total);
    }

    protected override void OnAbort()
    {
        _tracker.Abort();
    }
}