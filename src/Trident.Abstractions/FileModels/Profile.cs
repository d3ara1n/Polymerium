namespace Trident.Abstractions.FileModels;

public record struct Profile(
    string Name,
    string Version,
    IReadOnlyDictionary<string, object> Overrides,
    IEnumerable<Profile.Layer> Layers)
{
    public record struct Layer(
        bool Active,
        string Summary,
        string? Source,
        IEnumerable<string> Loaders,
        IEnumerable<string> Packages);
}