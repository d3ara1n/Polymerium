namespace Trident.Abstractions.Repositories;

public record RepositoryStatus(IReadOnlyList<string> SupportedLoaders, IReadOnlyList<string> SupportedVersions);