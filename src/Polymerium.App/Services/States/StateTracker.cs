using System;
using System.Reactive.Subjects;
using System.Threading;
using Polymerium.Trident;

namespace Polymerium.App.Services.States;

public class StateTracker(
    string key,
    InstanceState trigger,
    Subject<ITracklet> tracklets,
    CancellationToken token = default)
{
    internal readonly CancellationTokenSource CancellationTokenSource =
        CancellationTokenSource.CreateLinkedTokenSource(token);

    public CancellationToken Token => CancellationTokenSource.Token;
    public string Key => key;
    public InstanceState Trigger => trigger;

    public void Report(ITracklet tracklet) => tracklets.OnNext(tracklet);

    public void ReportState(InstanceState state) => Report(new StateTracklet(Key, state));
    public void ReportProgress(double? progress) => Report(new ProgressTracklet(Key, progress));
    public void ReportStage(string name) => Report(new StageTracklet(Key, name));
    public void ReportException(Exception ex) => Report(new ExceptionTracklet(Key, ex));
}