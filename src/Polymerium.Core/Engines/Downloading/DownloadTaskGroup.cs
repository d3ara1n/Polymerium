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
        public bool TryAdd(string source, string destintion, out DownloadTask task)
        {
            if (!tasks.Any(x => x.Destination == destintion))
            {
                var tmp = new DownloadTask()
                {
                    Source = source,
                    Destination = destintion,
                    Token = Token
                };
                tasks.Add(tmp);
                task = tmp;
                return true;
            }
            else
            {
                task = null;
                return false;
            }
        }
        public void Wait()
        {
            // 用 WaitAll 会有小问题
            if (tasks.Any())
            {
                waitHandle.WaitOne();
            }
        }
    }
}
