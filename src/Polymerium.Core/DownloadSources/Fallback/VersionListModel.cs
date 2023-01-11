using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.DownloadSources.Fallback
{
    internal class VersionListModel
    {
        public LatestModel Latest { get; set; }
        public IEnumerable<VersionModel> Versions { get; set; }
    }
}
