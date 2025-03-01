namespace Polymerium.Trident.Engines.Deploying.Stages;

public abstract class StageBase
{
    public DeployContext Context { get; set; } = null!;

    protected abstract Task OnProcessAsync(CancellationToken token);

    public Task ProcessAsync(CancellationToken token) => OnProcessAsync(token);
}