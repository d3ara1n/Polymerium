using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying;

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
    internal bool IsAborted;
    internal bool IsAttachmentResolved;
    internal bool IsFinished;
    internal bool IsGameInstalled;
    internal bool IsLoaderProcessed;
    internal bool IsSolidified;
    internal TransientData? Transient;

    public CancellationToken Token { get; } = token;
    public string Key { get; } = key;
    public Metadata Metadata { get; } = metadata;
    public ICollection<string> Keywords { get; } = keywords;

    public string Watermark { get; } =
        BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata))));

    public TridentContext Context { get; } = context;
    public JsonSerializerOptions SerializerOptions { get; } = options;

    public string ArtifactPath => Context.InstanceArtifactPath(Key);
}