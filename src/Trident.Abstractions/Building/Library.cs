namespace Trident.Abstractions.Building;

public record Library(
    string Name,
    string Path,
    Uri Url,
    string Sha1,
    bool IsNative = false,
    bool IsPresent = true)
{
}