using Microsoft.Extensions.Options;
using MirrorChyan.Net.Clients;
using MirrorChyan.Net.Exceptions;
using MirrorChyan.Net.Models;

namespace MirrorChyan.Net.Services;

internal class MirrorChyanService(IMirrorChyanClient client, IOptions<MirrorChyanOptions> options) : IMirrorChyanService
{
    public async Task<VersionModel> GetLatestVersionAsync(string? cdk, ChannelKind? channel)
    {
        var response = await client
                            .GetLatestVersionAsync(options.Value.ProductId,
                                                   options.Value.IsIncrementalEnabled
                                                       ? options.Value.VersionString
                                                       : null,
                                                   cdk,
                                                   options.Value.ClientName,
                                                   options.Value.Os,
                                                   options.Value.Arch,
                                                   Channels.FromKind(channel))
                            .ConfigureAwait(false);
        if (response.Code != 0)
        {
            throw response.Code switch
            {
                >= 7000 and < 8000 => new CdkNotAvailableException(response.Msg, cdk ?? "null"),
                8001 => new ProductIdNotFoundException(response.Msg, options.Value.ProductId),
                _ => new UnexpectedResponseCodeException(response.Msg, response.Code)
            };
        }

        return new(Channels.ToKind(response.Data.Channel),
                   response.Data.VersionName,
                   response.Data.VersionNumber,
                   response.Data.ReleaseNote,
                   response.Data is { Url: not null, Filesize: not null, Sha256: not null, UpdateType: not null }
                       ? new(response.Data.UpdateType.Value,
                             response.Data.Url,
                             response.Data.Sha256,
                             response.Data.Filesize.Value,
                             response.Data.Os,
                             response.Data.Arch)
                       : null);
    }
}
