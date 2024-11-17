namespace Trident.Abstractions.Repositories;

public record Filter(string? Version, string? ModLoader, PackageKind? Kind);