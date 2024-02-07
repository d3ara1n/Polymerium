using Microsoft.Extensions.Logging;

namespace Polymerium.Trident.Engines.Deploying;

public abstract class StageBase
{
    public DeployContext Context { get; set; } = null!;
    public ILogger Logger { get; set; } = null!;

    protected abstract Task OnProcessAsync();

    public async Task ProcessAsync()
    {
        try
        {
            await OnProcessAsync();
        }
        catch (Exception e)
        {
            Logger.LogError("Exception occurred: {message}", e.Message);
            throw new DeployException(this, e);
        }
    }
}