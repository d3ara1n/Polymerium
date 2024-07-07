using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Engines.Resolving;

public class ResolveException : Exception
{
    public ResolveException(Attachment attachment, Exception inner) : base(
        $"Failed to resolve \"{attachment.Label}:{attachment.ProjectId}/{attachment.VersionId ?? "N/A"}\": {inner.Message}",
        inner) =>
        Attachment = attachment;

    public Attachment Attachment { get; }
}