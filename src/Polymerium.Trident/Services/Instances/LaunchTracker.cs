using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class LaunchTracker(string key, TrackerHandler handler, Action<TrackerBase> onCompleted) : TrackerBase(key, handler, onCompleted)
{
}