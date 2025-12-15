using MirrorChyan.Net.Models;
using Refit;

namespace MirrorChyan.Net.Clients;

public interface IMirrorChyanClient
{
    [Get("/api/resources/{rid}/latest}")]
    Task<ObjectResponse<GetLatestVersionResponseBody>> GetLatestVersionAsync(
        string rid,
        string? currentVersion,
        string? cdk,
        string? userAgent,
        string? os,
        string? arch,
        string? channel);
}
