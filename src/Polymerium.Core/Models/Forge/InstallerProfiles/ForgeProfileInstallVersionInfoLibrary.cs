using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Forge.InstallerProfiles
{
    public struct ForgeProfileInstallVersionInfoLibrary
    {
        public string Name { get; set; }
        public Uri? Url { get; set; }
        [JsonProperty("clientreq")]
        public bool? ClientRequired { get; set; }
        [JsonProperty("serverreq")]
        public bool? ServerRequired { get; set; }
        public IEnumerable<string>? Checksums { get; set; }
    }
}
