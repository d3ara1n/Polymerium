using System.Reactive.Subjects;
using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances
{
    public class UpdateTracker(
        string key,
        Func<TrackerBase, Task> handler,
        Action<TrackerBase>? onCompleted,
        CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token)
    {
        public Subject<double?> ProgressStream { get; } = new();
        public string? OldSource { get; set; }
        public string? NewSource { get; set; }

        public override void Dispose()
        {
            base.Dispose();
            ProgressStream.Dispose();
        }
    }
}
