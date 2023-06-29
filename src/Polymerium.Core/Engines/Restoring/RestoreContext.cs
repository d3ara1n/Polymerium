using Polymerium.Abstractions.Models;
using Polymerium.Core.GameAssets;
using System.Collections.Generic;

namespace Polymerium.Core.Engines.Restoring;

public class RestoreContext
{
    public RestoreContext(PolylockData polylock)
    {
        Polylock = polylock;
        Tasks = new List<RestoreDownload>();
        MergedStates = new List<RenewableAssetState>();
    }

    public PolylockData Polylock { get; }
    public IList<RestoreDownload> Tasks { get; }
    public IList<RenewableAssetState> MergedStates { get; }
}
