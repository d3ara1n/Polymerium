using System;
using System.Threading;
using System.Threading.Tasks;
using Polymerium.Abstractions;

namespace Polymerium.Core.StageModels;

public delegate void TaskFinishedDelegate(string message, string? details = null);

public abstract class StageBase
{
    public abstract string StageNameKey { get; }
    public bool IsCompletedSuccessfully { get; protected set; }
    public CancellationToken Token { get; set; } = CancellationToken.None;
    public Exception? Exception { get; protected set; }
    public string? ErrorMessage { get; protected set; }
    public TaskFinishedDelegate? TaskFinishedCallback { get; set; }
    public abstract Task<Option<StageBase>> StartAsync();

    public Option<StageBase> Next(StageBase next)
    {
        IsCompletedSuccessfully = true;
        return Option<StageBase>.Some(next);
    }

    public Option<StageBase> Finish()
    {
        IsCompletedSuccessfully = true;
        return Option<StageBase>.None();
    }

    public Option<StageBase> Error(string errorMessage, Exception? exception = null)
    {
        IsCompletedSuccessfully = false;
        ErrorMessage = errorMessage;
        Exception = exception;
        return Option<StageBase>.None();
    }

    public Option<StageBase> Cancel()
    {
        return Finish();
    }

    public void Report(string message, string? details = null)
    {
        TaskFinishedCallback?.Invoke(message, details);
    }
}
