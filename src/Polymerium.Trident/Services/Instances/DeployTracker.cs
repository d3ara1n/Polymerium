using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class DeployTracker(
    string key,
    Func<TrackerBase, Task> handler,
    Action<TrackerBase>? onCompleted = null,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token),
                                         IProgress<DeployTracker.DeployProgress>
{
    public DeployProgress Progress { get; private set; }

    void IProgress<DeployProgress>.Report(DeployProgress value) => Progress = value;

    public record struct DeployProgress(string Message, double? Percentage = null);
}