using Polymerium.Trident.Models.MojangLauncherApi;
using Refit;

namespace Polymerium.Trident.Clients
{
    public interface IMojangLauncherClient
    {
        [Get("/news.json")]
        Task<MinecraftNewsResponse> GetNewsAsync();
    }
}
