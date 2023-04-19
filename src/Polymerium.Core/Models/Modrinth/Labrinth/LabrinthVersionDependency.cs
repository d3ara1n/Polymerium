using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Modrinth.Labrinth
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public struct LabrinthVersionDependency
    {
        public string? VersionId { get; set; }
        public string? ProjectId { get; set; }
        public string? FileName { get; set; }
        public string DependencyType { get; set; }
    }
}
