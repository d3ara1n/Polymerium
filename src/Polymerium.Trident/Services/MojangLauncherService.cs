using Polymerium.Trident.Clients;
using Polymerium.Trident.Models.MojangLauncherApi;

namespace Polymerium.Trident.Services;

public class MojangLauncherService(IMojangLauncherClient client)
{
    public const string ENDPOINT = "https://launchercontent.mojang.com";

    public async Task<MinecraftNewsResponse> GetMinecraftNewsAsync() => await client.GetNewsAsync();

    public Uri GetAbsoluteImageUrl(Uri imageUrl) => new(new Uri(ENDPOINT, UriKind.Absolute), imageUrl);
}