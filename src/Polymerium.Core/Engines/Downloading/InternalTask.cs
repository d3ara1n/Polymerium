using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Downloading
{
    internal class InternalTask
    {
        public InternalTaskGroup AssociatedGroup { get; set; }
        public DownloadTask Inner { get; set; }
        public int RetryCount { get; set; } = 0;
    }
}
