using System;
using System.Threading;

namespace Polymerium.Core.Engines.Downloading;

public class DownloadTask
{
    internal EventWaitHandle waitHandle = new(false, EventResetMode.ManualReset);
    public string Source { get; set; }
    public string Destination { get; set; }
    public bool IsSuccessful { get; set; }
    public Action<DownloadTask, bool> CompletedCallback { get; set; }
    public CancellationToken Token { get; set; } = CancellationToken.None;

    public bool Wait()
    {
        waitHandle.WaitOne();
        return IsSuccessful;
    }
}