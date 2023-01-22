using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct IndexDownloads
    {
        public IndexDownloadsItem Client { get; set; }
        public IndexDownloadsItem ClientMappings { get; set; }
        public IndexDownloadsItem Server { get; set; }
        public IndexDownloadsItem ServerMappings { get; set; }
    }
}
