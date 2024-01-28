using System.Reflection;
using DotNext.Collections.Generic;

namespace Trident.Abstractions.Tasks;

public abstract class TaskBase(string key)
{
    private readonly CancellationTokenSource source = new();

    private readonly IList<(WeakReference, MethodInfo)> subscribers = new List<(WeakReference, MethodInfo)>();
    protected CancellationToken Token => source.Token;
    public bool IsAborted => Token.IsCancellationRequested;
    public string Key => key;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;

    public TaskState State { get; private set; }
    public uint? Progress { get; private set; }

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

    protected void UpdateProgress(TaskState state, uint? progress = null)
    {
        var args = new TaskProgressUpdatedEventArgs(Key, state, progress);
        subscribers.Where(x => x.Item1.IsAlive).ForEach(x => x.Item2.Invoke(x.Item1.Target, [this, args]));
        State = state;
        Progress = progress;
    }

    public void Subscribe(Action<TaskBase, TaskProgressUpdatedEventArgs> callback)
    {
        subscribers.Add((new WeakReference(callback.Target), callback.Method));
    }
}