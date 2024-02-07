namespace Polymerium.Trident.Engines.Resolving;

public class ResolveException : Exception
{
    public ResolveException(string purl, Exception inner) : base($"Failed to resolve {purl}: {inner.Message}", inner)
    {
    }

    public ResolveException(string message) : base(message)
    {
    }
}