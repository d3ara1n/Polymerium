namespace Trident.Abstractions.Repositories.Resources;

public record Requirement(IEnumerable<string> AnyOfVersions, IEnumerable<string> AnyOfLoaders);