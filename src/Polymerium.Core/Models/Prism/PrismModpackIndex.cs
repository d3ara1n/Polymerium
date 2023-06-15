using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Prism
{
    public struct PrismModpackIndex
    {
        public IEnumerable<PrismModpackComponent> Components { get; set; }
        public uint FormatVersion { get; set; }
    }
}
