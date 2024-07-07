namespace Trident.Abstractions.Resources;

public record Dependency(string Label, string ProjectId, string? VersionId, bool Required)
{
}