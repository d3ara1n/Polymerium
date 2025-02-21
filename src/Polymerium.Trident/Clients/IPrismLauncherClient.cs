using Polymerium.Trident.Models.PrismLauncherApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IPrismLauncherClient
{
    [Get("/v1/{uid}/index.json")]
    Task<ComponentIndex> GetComponentIndexAsync(string uid);
}