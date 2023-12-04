using DotNext;

namespace Polymerium.Trident;

public readonly struct Package(string ProjectId, string ProjectName, string VersionId, string VersionName, string Author,
    string Summary, Uri Thumbnail, Uri Reference, Package.Kind Kind, string FileName, Uri Download,
    string? Hash, Package.Requirement Requirement, IEnumerable<Package.Dependency> Dependencies)
{
    public enum Kind
    {
        Modpack,
        Mod,
        World,
        DataPack,
        ResourcePack,
        ShaderPack
    }

    public readonly struct Dependency(string Purl, bool Required);

    public readonly struct Requirement(Optional<IEnumerable<string>> AnyOfVersions, Optional<IEnumerable<string>> AnyOfLoaders);
}