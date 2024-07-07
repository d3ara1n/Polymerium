using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Resolving;

public class ResolveContext(IEnumerable<Attachment> attachments)
{
    public IEnumerable<Attachment> Attachments { get; } = attachments;
}