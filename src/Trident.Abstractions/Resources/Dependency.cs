namespace Trident.Abstractions.Resources;

public record Dependency(string Purl, bool Required)
{
}