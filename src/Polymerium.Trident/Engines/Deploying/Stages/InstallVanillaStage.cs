using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Trident.Models.PrismLauncher.Minecraft;
using Trident.Abstractions.Building;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class InstallVanillaStage : StageBase
{
    private const string INDEX_URL = "https://meta.prismlauncher.org/v1/net.minecraft/index.json";
    private const string VERSION_URL = "https://meta.prismlauncher.org/v1/net.minecraft/{version}.json";

    public override async Task<bool> ProcessAsync()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var builder = Context.ArtifactBuilder ?? new ArtifactBuilder();
        var factory = Provider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient();
        var manifest = await client.GetFromJsonAsync<PrismMinecraftIndex>(INDEX_URL, options, Context.Token);
        if (manifest.Equals(default)) return false;
        var index = manifest.Versions.FirstOrDefault(x => x.Version == Context.Metadata.Version);
        if (index.Equals(default)) return false;
        var url = VERSION_URL.Replace("{version}", index.Version);
        var version = await client.GetFromJsonAsync<PrismMinecraftVersion>(url, options, Context.Token);
        if (version.Equals(default)) return false;

        // TODO

        Context.ArtifactBuilder ??= builder;
        return true;
    }
}