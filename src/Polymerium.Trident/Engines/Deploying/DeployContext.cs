using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Trident.Abstractions.Profiles;

namespace Polymerium.Trident.Engines.Deploying;

public class DeployContext
{
    public DeployContext(string key, Metadata metadata)
    {
        Key = key;
        Metadata = metadata;

        Watermark = BitConverter.ToString(MD5.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata))));
    }

    public string Key { get; init; }
    public Metadata Metadata { get; init; }
    public string Watermark { get; }
}