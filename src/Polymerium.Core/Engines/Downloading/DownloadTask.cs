using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Downloading
{
    public class DownloadTask
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public Action<DownloadTask, bool> CompletedCallback { get; set; }
        public CancellationToken Token { get; set; } = CancellationToken.None;
        internal EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        public void Wait()
        {
            waitHandle.WaitOne();
        }
    }
}
