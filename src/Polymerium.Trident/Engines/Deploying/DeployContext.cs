using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Trident.Abstractions;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying;

public class DeployContext
{
    public DeployContext(string key, Metadata metadata, CancellationToken token = default)
    {
        Token = token;
        Key = key;
        Metadata = metadata;
    
        Watermark = BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata))));
    }

    public CancellationToken Token { get; }
    public string Key { get; }
    public Metadata Metadata { get; }
    public string Watermark { get; }

    internal Artifact? Artifact;
    internal ArtifactBuilder? ArtifactBuilder;
}