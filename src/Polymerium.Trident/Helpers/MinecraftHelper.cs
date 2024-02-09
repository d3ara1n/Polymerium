using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polymerium.Trident.Models.PrismLauncher.Minecraft;
using Trident.Abstractions.Exceptions;

namespace Polymerium.Trident.Helpers;

public static class MinecraftHelper
{
    private const string INDEX_URL = "https://meta.prismlauncher.org/v1/net.minecraft/index.json";
    private const string VERSION_URL = "https://meta.prismlauncher.org/v1/net.minecraft/{version}.json";

    // 使用本地创建的 options，和 Polymerium 的文件不共用同一个
    private static readonly JsonSerializerOptions OPTIONS = new(JsonSerializerDefaults.Web);

    static MinecraftHelper()
    {
        OPTIONS.Converters.Add(new JsonStringEnumConverter());
    }

    public static async Task<PrismMinecraftIndex> GetManifestAsync(IHttpClientFactory factory,
        CancellationToken token = default)
    {
        using var client = factory.CreateClient();
        var manifest = await client.GetFromJsonAsync<PrismMinecraftIndex>(INDEX_URL, OPTIONS, token);
        if (manifest.Equals(default)) throw new BadFormatException($"File({INDEX_URL}) failed to download or parse");
        return manifest;
    }

    public static async Task<PrismMinecraftVersion> GetVersionAsync(string version, IHttpClientFactory factory,
        CancellationToken token = default)
    {
        using var client = factory.CreateClient();
        var url = VERSION_URL.Replace("{0}", version);
        var index = await client.GetFromJsonAsync<PrismMinecraftVersion>(url, OPTIONS, token);
        if (index.Equals(default)) throw new BadFormatException($"File({url}) failed to download or parse");
        return index;
    }
}