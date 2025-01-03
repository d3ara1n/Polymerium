using Trident.Abstractions.Repositories.Resources;

namespace Trident.Abstractions.Repositories;

public record Filter(string? Version, string? Loader, ResourceKind? Kind);