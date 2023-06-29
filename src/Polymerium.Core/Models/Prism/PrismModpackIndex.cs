using System.Collections.Generic;

namespace Polymerium.Core.Models.Prism
{
    public struct PrismModpackIndex
    {
        public IEnumerable<PrismModpackComponent> Components { get; set; }
        public uint FormatVersion { get; set; }
    }
}
