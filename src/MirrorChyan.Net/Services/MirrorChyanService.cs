using Microsoft.Extensions.Options;
using MirrorChyan.Net.Clients;
using MirrorChyan.Net.Exceptions;
using MirrorChyan.Net.Models;

namespace MirrorChyan.Net.Services;

public class MirrorChyanService(IMirrorChyanClient client, IOptions<MirrorChyanOptions> options)
{
    public async Task<VersionModel> GetLatestVersionAsync(string? cdk, string? os, string? arch, ChannelKind? channel)
    {
        var response = await client
                            .GetLatestVersionAsync(options.Value.ProductId,
                                                   options.Value.VersionString,
                                                   cdk,
                                                   options.Value.ClientName,
                                                   os,
                                                   arch,
                                                   Channels.FromKind(channel))
                            .ConfigureAwait(false);
        if (response.Code != 0)
        {
            throw new UnexpectedResponseCodeException(response.Msg, response.Code);
        }

        return new(response.Data.UpdateType,
                   Channels.ToKind(response.Data.Channel),
                   response.Data.VersionName,
                   response.Data.VersionNumber,
                   response.Data.ReleaseNote,
                   new(response.Data.Url,
                       response.Data.Sha256,
                       response.Data.FileSize,
                       response.Data.Os,
                       response.Data.Arch));
    }
}
