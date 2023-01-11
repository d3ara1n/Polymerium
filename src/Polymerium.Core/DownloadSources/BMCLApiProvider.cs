using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.DownloadSources;

namespace Polymerium.Core.DownloadSources
{
    public class BMCLApiProvider: DownloadSourceProviderBase
    {
        public override string Identity => "BMCLApi";
    }
}
