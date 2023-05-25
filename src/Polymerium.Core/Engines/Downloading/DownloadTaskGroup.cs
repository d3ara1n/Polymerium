using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Polymerium.Core.Engines.Downloading;

public class DownloadTaskGroup
{
    private readonly List<DownloadTask> tasks = new();

    internal int downloadedCount = 0;

    internal EventWaitHandle waitHandle = new(false, EventResetMode.ManualReset);
    public IEnumerable<DownloadTask> Tasks => tasks;

    public Action<DownloadTaskGroup, DownloadTask, int, bool>? CompletedDelegate { get; set; }

    public CancellationToken Token { get; set; } = CancellationToken.None;

    public int DownloadedCount => downloadedCount;
    public int TotalCount => tasks.Count;

    public bool TryAdd(string source, string destintion, out DownloadTask? task)
    {
        if (tasks.All(x => x.Destination != destintion))
        {
            var tmp = new DownloadTask
            {
                Source = source,
                Destination = destintion,
                Token = Token
            };
            tasks.Add(tmp);
            task = tmp;
            return true;
        }

        task = null;
        return false;
    }

    public bool Wait()
    {
        // 用 WaitAll 会有小问题
        if (tasks.Any())
            waitHandle.WaitOne();
        return tasks.All(x => x.IsSuccessful);
    }
}
