using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Mojang.Indexes
{
    public struct ArgumentsItem
    {
        public IEnumerable<Rule> Rules { get; set; }
        public IEnumerable<string> Values { get; set; }
    }
}
