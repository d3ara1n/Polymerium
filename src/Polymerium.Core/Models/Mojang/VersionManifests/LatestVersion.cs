using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.VersionManifests
{
    public struct LatestVersion
    {
        public string Snapshot { get; set; }
        public string Release { get; set; }
    }
}
