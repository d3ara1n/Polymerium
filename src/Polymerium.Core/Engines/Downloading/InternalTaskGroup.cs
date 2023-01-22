using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Downloading
{
    internal class InternalTaskGroup
    {
        public DownloadTaskGroup Inner { get; set; }

        public int Downloaded = 0;
    }
}
