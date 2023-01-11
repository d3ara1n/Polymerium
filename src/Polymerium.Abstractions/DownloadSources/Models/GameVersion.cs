using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.DownloadSources.Models
{
    public struct GameVersion
    {
        public GameVersion(string version, string releaseType, DateTimeOffset time, DateTimeOffset releaseTime)
        {
            Version = version;
            ReleaseType = releaseType;
            Time = time;
            ReleaseTime = releaseTime;
        }

        public string Version { get; private set; }
        public string ReleaseType { get; private set; }
        public DateTimeOffset Time { get; private set; }
        public DateTimeOffset ReleaseTime { get; private set; }
    }
}
