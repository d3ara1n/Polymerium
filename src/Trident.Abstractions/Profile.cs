namespace Trident.Abstractions;

public record Profile(string Version, IList<Profile.Loader> Loaders, IList<Profile.Layer> Attachments)
{
    public record Loader(string Id, string Version);
    public record Layer(bool Enabled, string Summary, string Source, IList<string> Content);
}