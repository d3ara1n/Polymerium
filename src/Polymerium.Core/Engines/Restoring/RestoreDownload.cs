using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Restoring
{
    public struct RestoreDownload
    {
        public Uri Source { get; set; }
        public Uri Target { get; set; }
        public Action<Uri>? PostAction { get; set; }
    }
}
