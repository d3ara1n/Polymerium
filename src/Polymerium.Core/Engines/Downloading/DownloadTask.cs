using System;
using System.Threading;

namespace Polymerium.Core.Engines.Downloading;

public class DownloadTask
{
    internal EventWaitHandle waitHandle = new(false, EventResetMode.ManualReset);
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public Action<DownloadTask, bool>? CompletedCallback { get; set; }
    public CancellationToken Token { get; set; } = CancellationToken.None;

    public bool Wait()
    {
        waitHandle.WaitOne();
        return IsSuccessful;
    }
}