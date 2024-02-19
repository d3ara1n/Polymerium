namespace Trident.Abstractions.Resources;

public record Attachment(string Label, string ProjectId, string? VersionId)
{
    public override string ToString()
    {
        return VersionId != null ? $"{Label}:{ProjectId}/{VersionId}" : $"{Label}:{ProjectId}/*";
    }
}