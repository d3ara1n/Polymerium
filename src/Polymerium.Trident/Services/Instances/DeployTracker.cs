using Trident.Abstractions;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class DeployTracker(TrackerHandler handler, Action<TrackerBase> onCompleted, string key, Metadata metadata)
    : TrackerBase(key, handler, onCompleted)
{
    public Metadata Metadata => metadata;

    public event DeployStageUpdatedHandler? StageUpdated;
    public event DeployFileSolidifiedHandler? FileSolidified;

    internal void OnStageUpdate(DeployStage stage)
    {
        StageUpdated?.Invoke(this, stage);
    }

    internal void OnFileSolidified(string fileName, uint finished, uint total)
    {
        FileSolidified?.Invoke(this, finished, total);
    }
}