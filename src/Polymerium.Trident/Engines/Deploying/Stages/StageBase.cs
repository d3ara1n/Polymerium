using System.Reactive.Disposables;
using Trident.Abstractions.Reactive;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public abstract class StageBase : IDisposableLifetime
{
    public DeployContext Context { get; set; } = null!;
    public CompositeDisposable DisposableLifetime { get; } = new();

    protected abstract Task OnProcessAsync(CancellationToken token);

    public Task ProcessAsync(CancellationToken token) => OnProcessAsync(token);

    public virtual void Dispose()
    {
        DisposableLifetime.Dispose();
    }
}