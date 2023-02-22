using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Forge.InstallerProfiles
{
    public struct ForgeProfileInstallVersionInfo
    {
        public string id { get; set; }
        public DateTimeOffset Time { get; set; }
        public DateTimeOffset ReleaseTime { get; set; }
        public string Type { get; set; }
        public string MinecraftArguments { get; set; }
        public IEnumerable<ForgeProfileInstallVersionInfoLibrary> Libraries { get; set; }
        public string MainClass { get; set; }
        public int MinimumLauncherVersion { get; set; }
        public bool Synced { get; set; }
    }
}
