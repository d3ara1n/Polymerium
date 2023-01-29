using System;

namespace Polymerium.Core.Models.Mojang.VersionManifests
{
    public struct Version
    {
        public string Id { get; set; }
        public ReleaseType Type { get; set; }
        public Uri Url { get; set; }
        public DateTimeOffset Time { get; set; }
        public DateTimeOffset ReleaseTime { get; set; }
    }
}