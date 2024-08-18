using System;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Tasks;

public class TaskProgressUpdatedEventArgs(string key, TaskState state, string stage, string status, uint? progress)
    : EventArgs
{
    public string Key { get; init; } = key;
    public TaskState State { get; init; } = state;
    public string Stage { get; init; } = stage;
    public string Status { get; init; } = status;
    public uint? Progress { get; init; } = progress;
}