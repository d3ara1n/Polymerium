namespace Polymerium.Core.Engines.Downloading;

internal class InternalTaskGroup
{
    public InternalTaskGroup(DownloadTaskGroup inner)
    {
        Inner = inner;
    }

    public DownloadTaskGroup Inner { get; set; }
}