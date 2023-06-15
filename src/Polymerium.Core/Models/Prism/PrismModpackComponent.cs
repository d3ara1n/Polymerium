using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Models.Prism
{
    public struct PrismModpackComponent
    {
        public bool? Important { get; set; }
        public string Uid { get; set; }
        public string Version { get; set; }
    }
}
