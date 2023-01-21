using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Meta
{
    public struct GameMetadata
    {
        public string CoreVersion { get; set; }
        public IEnumerable<Component> Components { get; set; }
        public IEnumerable<Uri> Attachments { get; set; }
    }
}
