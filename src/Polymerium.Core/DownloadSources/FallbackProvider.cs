using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Polymerium.Abstractions;
using Polymerium.Abstractions.DownloadSources;
using Polymerium.Abstractions.DownloadSources.Models;
using Polymerium.Core.DownloadSources.Fallback;
using Wupoo;

namespace Polymerium.Core.DownloadSources
{
    public class FallbackProvider : DownloadSourceProviderBase
    {
        public override string Identity => "FALLBACK";

        public async override Task<Option<IEnumerable<GameVersion>>> GetGameVersionsAsync()
        {
            VersionListModel model = null;
            await Wapoo.Wohoo("https://piston-meta.mojang.com/mc/game/version_manifest.json")
                .ForJsonResult<VersionListModel>(v => model = v)
                .FetchAsync();
            if (model == null)
            {
                return Option<IEnumerable<GameVersion>>.None();
            }
            else
            {
                var versions = model.Versions.Select(it => new GameVersion(it.Id, it.Type switch
                {
                    "snapshot" => ReleaseType.Snapshot,
                    "old_beta" => ReleaseType.Beta,
                    "old_alpha" => ReleaseType.Alpha,
                    _ => ReleaseType.Release
                }, it.Time, it.ReleaseTime));
                return Option<IEnumerable<GameVersion>>.Some(versions);
            }
        }
    }
}
