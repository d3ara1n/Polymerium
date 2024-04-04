using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using System.Text.Json;
using Trident.Abstractions;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying
{
    public class DeployContext(
        TridentContext context,
        string key,
        Metadata metadata,
        ICollection<string> keywords,
        JsonSerializerOptions options,
        CancellationToken token = default)
    {
        internal Artifact? Artifact;
        internal ArtifactBuilder? ArtifactBuilder;
        internal bool IsAttachmentResolved;
        internal bool IsGameInstalled;
        internal bool IsLoaderProcessed;
        internal bool IsSolidified;
        internal TransientData? Transient;

        public CancellationToken Token { get; } = token;
        public string Key { get; } = key;
        public Metadata Metadata { get; } = metadata;
        public ICollection<string> Keywords { get; } = keywords;

        public string Watermark { get; } = metadata.ComputeWatermark();

        public TridentContext Trident { get; } = context;
        public JsonSerializerOptions SerializerOptions { get; } = options;

        public string ArtifactPath => Trident.InstanceArtifactPath(Key);
    }
}