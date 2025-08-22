using Polymerium.Trident.Models.XboxLiveApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IXboxServiceClient
{
    [Post("/xsts/authorize")]
    Task<XboxLiveResponse> AcquireMinecraftTokenAsync([Body] XboxLiveRequest<MinecraftTokenProperties> request);
}