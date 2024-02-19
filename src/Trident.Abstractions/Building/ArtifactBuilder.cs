using IBuilder;

namespace Trident.Abstractions.Building
{
    public class ArtifactBuilder : IBuilder<Artifact>
    {
        private readonly IList<string> gameArguments = new List<string>();
        private readonly IList<string> jvmArguments = new List<string>();

        private readonly IList<Artifact.Processor> processors = new List<Artifact.Processor>();
        private Artifact.AssetData? assetIndex;
        private uint? javaMajorVersion;
        private string? mainClassPath;

        private Artifact.ViabilityData? viabilityData;

        public IList<Artifact.Library> Libraries { get; } = new List<Artifact.Library>();

        public IList<Artifact.Parcel> Parcels { get; } = new List<Artifact.Parcel>();

        public Artifact Build()
        {
            ArgumentNullException.ThrowIfNull(viabilityData);
            ArgumentNullException.ThrowIfNull(assetIndex);
            ArgumentNullException.ThrowIfNull(javaMajorVersion);
            ArgumentNullException.ThrowIfNull(mainClassPath);
            Artifact artifact = new(viabilityData, mainClassPath, javaMajorVersion.Value, assetIndex,
                gameArguments,
                jvmArguments, Libraries, Parcels, processors);
            return artifact;
        }

        public ArtifactBuilder SetViability(Artifact.ViabilityData viability)
        {
            viabilityData = viability;
            return this;
        }

        public ArtifactBuilder ClearGameArguments()
        {
            gameArguments.Clear();
            return this;
        }

        public ArtifactBuilder AddGameArgument(string arg)
        {
            arg = arg.Trim();
            gameArguments.Add(arg);
            return this;
        }

        public ArtifactBuilder AddJvmArgument(string arg)
        {
            arg = arg.Trim();
            jvmArguments.Add(arg);
            return this;
        }

        public ArtifactBuilder AddParcel(Artifact.Parcel parcel)
        {
            Parcels.Add(parcel);
            return this;
        }

        public ArtifactBuilder AddLibrary(Artifact.Library library)
        {
            Libraries.Add(library);
            return this;
        }

        public ArtifactBuilder AddProcessor(Artifact.Processor processor)
        {
            processors.Add(processor);
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
}