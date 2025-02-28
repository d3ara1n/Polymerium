using Microsoft.Extensions.Logging;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public abstract class StageBase
{
    public DeployContext Context { get; set; } = null!;

    protected abstract Task OnProcessAsync(CancellationToken token);

    public Task ProcessAsync(CancellationToken token)
    {
        return OnProcessAsync(token);
    }
}