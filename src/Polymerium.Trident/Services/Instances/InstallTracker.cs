using System.Reactive.Subjects;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class InstallTracker(
    string key,
    Func<TrackerBase, Task> handler,
    Action<TrackerBase>? onCompleted = null,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token)
{
    public Subject<double?> ProgressStream { get; } = new();

    public override void Dispose()
    {
        base.Dispose();
        ProgressStream.Dispose();
    }
}