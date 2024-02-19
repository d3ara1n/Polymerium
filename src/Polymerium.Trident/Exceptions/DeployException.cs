using Polymerium.Trident.Engines.Deploying;

namespace Polymerium.Trident.Exceptions;

public class DeployException : Exception
{
    public DeployException(StageBase stage, Exception inner) : base(
        $"{stage.GetType().Name} threw an exception: {inner.Message}", inner)
    {
        Stage = stage;
    }
    public StageBase Stage { get; }
}