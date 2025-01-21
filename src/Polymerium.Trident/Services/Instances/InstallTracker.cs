using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class InstallTracker(
    string key,
    TrackerHandler handler,
    Action<TrackerBase>? onCompleted = null,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token)
{
}