namespace Trident.Abstractions;

// C# 12 的主构造器好多库不支持，故不适用

public record Profile(string Version, IList<Profile.Loader> Loaders, IList<Profile.Layer> Attachments)
{
    public record Loader(string Id, string Version);
    public record Layer(bool Enabled, string Summary, string Source, IList<string> Content);
}