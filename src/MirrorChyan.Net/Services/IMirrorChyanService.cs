using MirrorChyan.Net.Models;

namespace MirrorChyan.Net.Services;

public interface IMirrorChyanService
{
    Task<VersionModel> GetLatestVersionAsync(string? cdk, ChannelKind? channel);
}
