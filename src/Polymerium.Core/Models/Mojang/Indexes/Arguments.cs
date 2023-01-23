using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct Arguments
    {
        public IEnumerable<ArgumentsItem> Game { get; set; }
        public IEnumerable<ArgumentsItem> Jvm { get; set; }
    }
}
