using Trident.Abstractions.Repositories.Resources;

namespace Trident.Abstractions.Repositories;

public record RepositoryStatus(
    IReadOnlyList<string> SupportedLoaders,
    IReadOnlyList<string> SupportedVersions,
    IReadOnlyList<ResourceKind> SupportedKinds);