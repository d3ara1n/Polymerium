using DotNext.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Tasks;

public abstract class TaskBase(string key, string stage, string status)
{
    private readonly IList<(WeakReference, MethodInfo)> subscribers = new List<(WeakReference, MethodInfo)>();
    public string Key => key;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    public TaskState State { get; private set; } = TaskState.Idle;
    public uint? Progress { get; private set; }
    public string Stage { get; private set; } = stage;
    public string Status { get; private set; } = status;
    public Exception? FailureReason { get; protected set; }


    public void Abort() => OnAbort();

    protected virtual void OnAbort()
    {
    }


    protected void ReportProgress(uint? progress = null, string? stage = null, string? status = null) =>
        UpdateProgress(TaskState.Running, progress, stage, status);

    protected void UpdateProgress(TaskState state, uint? progress = null, string? stage = null,
        string? status = null, Exception? failure = null)
    {
        if (failure != null)
        {
            FailureReason = failure;
        }

        TaskProgressUpdatedEventArgs args = new(Key, state, stage ?? Stage, status ?? Status, progress);
        subscribers.Where(x => x.Item1.IsAlive).ForEach(x => x.Item2.Invoke(x.Item1.Target, [this, args]));
        State = state;
        Progress = progress;
        Stage = stage ?? Stage;
        Status = status ?? Status;
    }

    public void Subscribe(Action<TaskBase, TaskProgressUpdatedEventArgs> callback) =>
        subscribers.Add((new WeakReference(callback.Target), callback.Method));

    public void Unsubscribe(Action<TaskBase, TaskProgressUpdatedEventArgs> callback)
    {
        var first =
            subscribers.FirstOrDefault(x => x.Item1 == callback.Target && x.Item2 == callback.Method);
        if (!first.Equals(default))
        {
            subscribers.Remove(first);
        }
    }
}