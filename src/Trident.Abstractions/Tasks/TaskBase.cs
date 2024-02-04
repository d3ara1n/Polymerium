using System.Reflection;
using DotNext.Collections.Generic;

namespace Trident.Abstractions.Tasks;

public abstract class TaskBase(string key, string stage, string status)
{
    private readonly CancellationTokenSource source = new();

    private readonly IList<(WeakReference, MethodInfo)> subscribers = new List<(WeakReference, MethodInfo)>();
    protected CancellationToken Token => source.Token;
    public bool IsAborted => Token.IsCancellationRequested;
    public string Key => key;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    public TaskState State { get; private set; } = TaskState.Idle;
    public uint? Progress { get; private set; }
    public string Stage { get; private set; } = stage;
    public string Status { get; private set; } = status;

    public void Start()
    {
        if (State != TaskState.Idle && !Token.IsCancellationRequested) return;
        OnStart();
    }

    public void Abort()
    {
        if (IsAborted) return;
        source.Cancel();
        OnAbort();
    }

    protected virtual void OnAbort()
    {
        UpdateProgress(TaskState.Aborted);
    }

    protected virtual void OnStart()
    {
        UpdateProgress(TaskState.Running);
        var task = Task.Run(OnThreadAsync, Token);
        task.ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully)
                OnFinish();
            else if (t.IsCanceled)
                OnAbort();
            else if (t.IsFaulted)
                OnFault(t.Exception);
            else
                throw new NotImplementedException();
        }, Token);
    }

    protected virtual void OnFinish()
    {
        UpdateProgress(TaskState.Finished);
    }

    protected virtual void OnFault(Exception? error)
    {
        UpdateProgress(TaskState.Faulted);
    }

    protected abstract Task OnThreadAsync();

    protected void ReportProgress(uint? progress = null, string? stage = null, string? status = null)
    {
        UpdateProgress(TaskState.Running, progress, stage, status);
    }

    protected void UpdateProgress(TaskState state, uint? progress = null, string? stage = null, string? status = null)
    {
        var args = new TaskProgressUpdatedEventArgs(Key, state, stage ?? Stage, status ?? Status, progress);
        subscribers.Where(x => x.Item1.IsAlive).ForEach(x => x.Item2.Invoke(x.Item1.Target, [this, args]));
        State = state;
        Progress = progress;
        Stage = stage ?? Stage;
        Status = status ?? Status;
    }

    public void Subscribe(Action<TaskBase, TaskProgressUpdatedEventArgs> callback)
    {
        subscribers.Add((new WeakReference(callback.Target), callback.Method));
    }
}