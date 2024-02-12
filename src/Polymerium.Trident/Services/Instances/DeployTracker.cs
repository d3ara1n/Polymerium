using Trident.Abstractions;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class DeployTracker(TrackerHandler handler,Action<TrackerBase> onCompleted, string key, Metadata metadata)
    : TrackerBase(key, handler, onCompleted)
{
    public Metadata Metadata => metadata;

    public event DeployStageUpdatedHandler? StageUpdated;

    internal void OnStageUpdate(DeployStage stage)
    {
        StageUpdated?.Invoke(this, stage);
    }
}