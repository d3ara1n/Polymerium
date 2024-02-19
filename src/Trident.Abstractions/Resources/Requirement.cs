namespace Trident.Abstractions.Resources
{
    public record Requirement(IEnumerable<string> AnyOfVersions, IEnumerable<string> AnyOfLoaders);
}