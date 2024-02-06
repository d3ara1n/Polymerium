using Trident.Abstractions.Building;

namespace Trident.Abstractions.Extensions;

public static class ArtifactBuilderExtensions
{
    public static ArtifactBuilder AddFragile(this ArtifactBuilder self, string source, string target, Uri url,
        string sha1)
    {
        return self.AddParcel(new Parcel(source, target, url, sha1, true));
    }

    public static ArtifactBuilder AddPersistent(this ArtifactBuilder self, string source, string target)
    {
        return self.AddParcel(new Parcel(source, target, null, null, false));
    }


    public static ArtifactBuilder AddLibrary(this ArtifactBuilder self, string name, string path, Uri url, string sha1,
        bool native = false,
        bool present = true)
    {
        return self.AddLibrary(new Library(name, path, url, sha1, native, present));
    }
}