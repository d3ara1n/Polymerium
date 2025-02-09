using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class UpdateTracker(
    string key,
    Func<TrackerBase, Task> handler,
    Action<TrackerBase>? onCompleted,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token)
{
}