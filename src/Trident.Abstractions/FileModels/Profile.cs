namespace Trident.Abstractions.FileModels;

public record struct Profile(
    string Name,
    Profile.Setup Base,
    IReadOnlyDictionary<string, object> Overrides,
    IEnumerable<Profile.Layer> Layers)
{
    public record struct Setup(string? Source, string? Version, string? Loader, IEnumerable<string> Packages);

    public record struct Layer(
        bool Active,
        string Summary,
        IEnumerable<string> Packages);
}