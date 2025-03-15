using ObservableCollections;
using Polymerium.Trident.Engines.Launching;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class LaunchTracker(
    string key,
    Func<TrackerBase, Task> handler,
    Action<TrackerBase>? onCompleted,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token)
{
    public ObservableFixedSizeRingBuffer<Scrap> ScrapBuffer { get; } = new(9527);

    public bool IsDetaching { get; set; } = false;

    public override void Dispose()
    {
        base.Dispose();
        ScrapBuffer.Clear();
    }
}