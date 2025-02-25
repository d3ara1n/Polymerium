using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class UpdateTracker(
    string key,
    Func<TrackerBase, Task> handler,
    Action<TrackerBase>? onCompleted,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token), IProgress<double?>
{
    public double? Progress { get; private set; }

    #region IProgress<double?> Members

    void IProgress<double?>.Report(double? value) => Progress = value;

    #endregion
}