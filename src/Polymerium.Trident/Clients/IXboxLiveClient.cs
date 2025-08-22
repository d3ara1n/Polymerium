using Polymerium.Trident.Models.XboxLiveApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IXboxLiveClient
{
    [Post("/user/authenticate")]
    Task<XboxLiveResponse> AcquireXboxLiveTokenAsync([Body] XboxLiveRequest<XboxLiveTokenProperties> request);
}