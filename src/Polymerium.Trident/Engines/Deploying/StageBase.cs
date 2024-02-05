using Microsoft.Extensions.Logging;

namespace Polymerium.Trident.Engines.Deploying;

public abstract class StageBase
{
    public DeployContext Context { get; set; } = null!;
    public ILogger Logger { get; set; } = null!;
    public IServiceProvider Provider { get; set; } = null!;

    public abstract Task<bool> ProcessAsync();
}