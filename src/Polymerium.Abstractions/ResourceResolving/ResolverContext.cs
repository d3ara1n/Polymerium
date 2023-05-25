namespace Polymerium.Abstractions.ResourceResolving;

public class ResolverContext
{
    public ResolverContext(GameInstance instance)
    {
        Instance = instance;
    }

    public ResolverContext()
    {
        Instance = null;
    }

    public GameInstance? Instance { get; }
}
