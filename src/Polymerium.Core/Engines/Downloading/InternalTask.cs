namespace Polymerium.Core.Engines.Downloading;

internal class InternalTask
{
    public InternalTask(DownloadTask inner, InternalTaskGroup? group = null)
    {
        Inner = inner;
        AssociatedGroup = group;
    }

    public InternalTaskGroup? AssociatedGroup { get; set; }
    public DownloadTask Inner { get; set; }
    public int RetryCount { get; set; } = 0;
}