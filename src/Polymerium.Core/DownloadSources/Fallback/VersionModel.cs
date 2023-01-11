using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.DownloadSources.Fallback
{
    internal class VersionModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public DateTimeOffset Time { get; set; }
        public DateTimeOffset ReleaseTime { get; set; }
        public string Url { get; set; }
    }
}
