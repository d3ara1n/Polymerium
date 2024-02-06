using IBuilder;

namespace Trident.Abstractions.Building;

public class ArtifactBuilder : IBuilder<Artifact>
{
    private readonly IList<string> gameArguments = new List<string>();
    private readonly IList<string> jvmArguments = new List<string>();

    private readonly IList<Library> libraries = new List<Library>();

    private readonly IList<Parcel> parcels = new List<Parcel>();

    public Artifact Build()
    {
        throw new NotImplementedException();
    }

    public ArtifactBuilder AppendGameArgument(string arg)
    {
        arg = arg.Trim();
        if (!gameArguments.Contains(arg))
            gameArguments.Add(arg);
        return this;
    }

    public ArtifactBuilder AppendJvmArgument(string arg)
    {
        arg = arg.Trim();
        if (!jvmArguments.Contains(arg))
            jvmArguments.Add(arg);
        return this;
    }

    public ArtifactBuilder AddParcel(Parcel parcel)
    {
        parcels.Add(parcel);
        return this;
    }

    public ArtifactBuilder AddLibrary(Library library)
    {
        libraries.Add(library);
        return this;
    }
}