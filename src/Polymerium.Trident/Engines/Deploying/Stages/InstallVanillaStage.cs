using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Polymerium.Trident.Engines.Deploying.Stages;

public class InstallVanillaStage : StageBase
{
    private const string META_URL = "https://piston-meta.mojang.com/mc/game/version_manifest.json";

    public override async Task<bool> ProcessAsync()
    {
        var factory = Provider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient();
        
        return true;
    }
}