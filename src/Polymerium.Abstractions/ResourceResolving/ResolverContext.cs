namespace Polymerium.Abstractions.ResourceResolving;

public class ResolverContext
{
    public ResolverContext(GameInstance instance)
    {
        Instance = instance;
    }

    public GameInstance Instance { get; }
}
