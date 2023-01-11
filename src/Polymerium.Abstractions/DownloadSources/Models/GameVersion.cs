using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.DownloadSources.Models
{
    public struct GameVersion
    {
        public GameVersion(string version, ReleaseType type, DateTimeOffset time, DateTimeOffset releaseTime)
        {
            Id = version;
            Type = type;
            Time = time;
            ReleaseTime = releaseTime;
        }

        public string Id { get; private set; }
        public ReleaseType Type { get; private set; }
        public DateTimeOffset Time { get; private set; }
        public DateTimeOffset ReleaseTime { get; private set; }

        public override string ToString()
        {
            return Id;
        }
    }
}
