using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
