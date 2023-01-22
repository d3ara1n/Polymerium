using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Polymerium.Core.Engines.Downloading
{
    public class DownloadTaskGroup
    {
        private readonly List<DownloadTask> tasks = new();
        public IEnumerable<DownloadTask> Tasks => tasks;

        public Action<DownloadTaskGroup, DownloadTask, int, bool> CompletedDelegate { get; set; }

        public CancellationToken Token { get; set; } = CancellationToken.None;

        public int TotalCount => tasks.Count;

        internal EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        public void Add(string source, string destintion)
        {
            if (!tasks.Any(x => x.Destination == destintion))
            {
                tasks.Add(new DownloadTask()
                {
                    Source = source,
                    Destination = destintion,
                    Token = Token
                });
            }

        }
        public void Wait()
        {
            // 用 WaitAll 会有小问题
            waitHandle.WaitOne();
        }
    }
}
