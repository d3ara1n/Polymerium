using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Polymerium.Abstractions;
using Polymerium.Core.Components;

namespace Polymerium.Core.Extensions;

public static class GameInstanceExtensions
{
    public static string ComputeMetadataHash(this GameInstance instance)
    {
        var json = JsonConvert.SerializeObject(instance.Metadata);
        var md5 = MD5.HashData(Encoding.UTF8.GetBytes(json)).Select(x => x.ToString("x"));
        return string.Join(string.Empty, md5);
    }

    public static string? GetCoreVersion(this GameInstance instance)
    {
        return instance.Metadata.Components.Any(x => x.Identity == ComponentMeta.MINECRAFT)
            ? instance.Metadata.Components.First(x => x.Identity == ComponentMeta.MINECRAFT).Version
            : null;
    }

    public static Uri GetPolylockDataUrl(this GameInstance instance)
    {
        return new Uri(ConstPath.INSTANCE_POLYLOCKDATA_FILE.Replace("{0}", instance.Id));
    }

    public static Uri GetPolylockHashUrl(this GameInstance instance)
    {
        return new Uri(ConstPath.INSTANCE_POLYLOCKHASH_FILE.Replace("{0}", instance.Id));
    }

    public static bool CheckIfRestored(this GameInstance instance, IFileBaseService fileBase, out string? content)
    {
        content = null;
        return fileBase.TryReadAllText(instance.GetPolylockHashUrl(), out var hash)
               && hash == instance.ComputeMetadataHash() &&
               fileBase.TryReadAllText(instance.GetPolylockDataUrl(), out content);
    }
}