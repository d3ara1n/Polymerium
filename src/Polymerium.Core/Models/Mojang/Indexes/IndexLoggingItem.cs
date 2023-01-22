using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct IndexLoggingItem
    {
        public string Arguments { get; set; }
        public IndexLoggingItemFile File { get; set; }
        public string Type { get; set; }
    }
}
