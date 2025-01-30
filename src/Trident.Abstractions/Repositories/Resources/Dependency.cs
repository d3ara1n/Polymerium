namespace Trident.Abstractions.Repositories.Resources;

public record Dependency(string Label, string? Namespace, string Pid, string? Vid, bool IsRequired);