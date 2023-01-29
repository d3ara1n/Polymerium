using Polymerium.Core.Models.Mojang.VersionManifests;
using System.Collections.Generic;

namespace Polymerium.Core.Models.Mojang
{
    public struct VersionManifest
    {
        public LatestVersion Latest { get; set; }
        public IEnumerable<Version> Versions { get; set; }
    }
}