using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;
using VelopackExtension.MirrorChyan.Sources;

namespace Polymerium.App.Services;

public class UpdateSourceSelector(
    IEnumerable<IUpdateSource> sources,
    ConfigurationService configurationService) : IUpdateSource
{
    #region IUpdateSource Members

    public Task<VelopackAssetFeed> GetReleaseFeed(
        IVelopackLogger logger,
        string? appId,
        string channel,
        Guid? stagingId = null,
        VelopackAsset? latestLocalRelease = null) =>
        Select().GetReleaseFeed(logger, appId, channel, stagingId, latestLocalRelease);

    public Task DownloadReleaseEntry(
        IVelopackLogger logger,
        VelopackAsset releaseEntry,
        string localFile,
        Action<int> progress,
        CancellationToken cancelToken = new()) =>
        Select().DownloadReleaseEntry(logger, releaseEntry, localFile, progress, cancelToken);

    #endregion

    private IUpdateSource Select()
    {
        // 0 => Github
        // 1 => MirrorChyan
        if (configurationService.Value.UpdateSource == 0)
        {
            return sources.OfType<GithubSource>().First();
        }
        else
        {
            return sources.OfType<MirrorChyanSource>().First();
        }
    }
}
