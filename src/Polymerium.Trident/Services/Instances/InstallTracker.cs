﻿using Trident.Abstractions.Tasks;

namespace Polymerium.Trident.Services.Instances;

public class InstallTracker(
    string key,
    Func<TrackerBase, Task> handler,
    Action<TrackerBase>? onCompleted = null,
    CancellationToken token = default) : TrackerBase(key, handler, onCompleted, token), IProgress<double?>
{
    public double? Progress { get; private set; }

    void IProgress<double?>.Report(double? value)
    {
        Progress = value;
    }
}