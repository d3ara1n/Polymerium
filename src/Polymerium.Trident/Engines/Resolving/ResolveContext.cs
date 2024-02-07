namespace Polymerium.Trident.Engines.Resolving;

public class ResolveContext(IEnumerable<string> attachments)
{
    public IEnumerable<string> Attachments { get; } = attachments;
}