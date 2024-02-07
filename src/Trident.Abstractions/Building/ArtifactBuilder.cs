using IBuilder;

namespace Trident.Abstractions.Building;

public class ArtifactBuilder : IBuilder<Artifact>
{
    private readonly IList<string> gameArguments = new List<string>();
    private readonly IList<string> jvmArguments = new List<string>();

    private readonly IList<Artifact.Library> libraries = new List<Artifact.Library>();
    private readonly IList<Artifact.Parcel> parcels = new List<Artifact.Parcel>();

    private readonly IList<Artifact.Processor> processors = new List<Artifact.Processor>();
    private Artifact.AssetData? assetIndex;
    private uint? javaMajorVersion;
    private string? mainClassPath;

    private Artifact.ViabilityData? viabilityData;

    public Artifact Build()
    {
        ArgumentNullException.ThrowIfNull(viabilityData);
        ArgumentNullException.ThrowIfNull(assetIndex);
        ArgumentNullException.ThrowIfNull(javaMajorVersion);
        ArgumentNullException.ThrowIfNull(mainClassPath);
        var artifact = new Artifact(viabilityData, mainClassPath, javaMajorVersion.Value, assetIndex, gameArguments,
            jvmArguments, libraries, parcels, processors);
        return artifact;
    }

    public ArtifactBuilder SetViability(Artifact.ViabilityData viability)
    {
        viabilityData = viability;
        return this;
    }

    public ArtifactBuilder AddGameArgument(string arg)
    {
        arg = arg.Trim();
        if (!gameArguments.Contains(arg))
            gameArguments.Add(arg);
        return this;
    }

    public ArtifactBuilder AddJvmArgument(string arg)
    {
        arg = arg.Trim();
        if (!jvmArguments.Contains(arg))
            jvmArguments.Add(arg);
        return this;
    }

    public ArtifactBuilder AddParcel(Artifact.Parcel parcel)
    {
        parcels.Add(parcel);
        return this;
    }

    public ArtifactBuilder AddLibrary(Artifact.Library library)
    {
        libraries.Add(library);
        return this;
    }

    public ArtifactBuilder SetAssetIndex(Artifact.AssetData index)
    {
        assetIndex = index;
        return this;
    }

    public ArtifactBuilder SetJavaMajorVersion(uint version)
    {
        javaMajorVersion = version;
        return this;
    }

    public ArtifactBuilder SetMainClass(string mainClass)
    {
        mainClassPath = mainClass;
        return this;
    }
}