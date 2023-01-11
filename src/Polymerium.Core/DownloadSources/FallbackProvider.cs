using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions;
using Polymerium.Abstractions.DownloadSources;
using Polymerium.Abstractions.DownloadSources.Models;

namespace Polymerium.Core.DownloadSources
{
    public class FallbackProvider : DownloadSourceProviderBase
    {
        public override string Identity => "FALLBACK";

        public override Option<IEnumerable<GameVersion>> GetGameVersions()
        {
            return base.GetGameVersions();
        }
    }
}
