namespace Trident.Abstractions.Tasks;

public abstract class TrackerBase(
    string key,
    TrackerHandler handler,
    Action<TrackerBase> onCompleted,
    CancellationToken token = default)
{
    private readonly CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
    public string Key => key;
    public CancellationToken Token => tokenSource.Token;
    public TaskState State { get; private set; } = TaskState.Idle;
    public Exception? FailureReason { get; private set; }

    public event TrackerStateUpdatedHandler? StateUpdated;

    public void Abort() => tokenSource.Cancel();

    public void Start() => OnStart();

    protected virtual void OnStart()
    {
        State = TaskState.Running;
        StateUpdated?.Invoke(this, State);
        Task.Run(async () => await handler(this), Token)
            .ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    OnFinish();
                }
                else if (t.IsCanceled)
                {
                    OnFault(new OperationCanceledException());
                }
                else if (t.IsFaulted)
                {
                    OnFault(t.Exception);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }, CancellationToken.None);
    }

    protected virtual void OnFinish()
    {
        State = TaskState.Finished;
        StateUpdated?.Invoke(this, State);
        onCompleted?.Invoke(this);
    }

    protected virtual void OnFault(Exception e)
    {
        FailureReason = e;
        State = TaskState.Faulted;
        StateUpdated?.Invoke(this, State);
        onCompleted?.Invoke(this);
    }
}