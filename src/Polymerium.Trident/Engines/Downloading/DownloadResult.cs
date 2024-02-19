namespace Polymerium.Trident.Engines.Downloading
{
    public record DownloadResult(
        string Target,
        Uri Source,
        string? Sha1,
        uint Index,
        uint Total,
        DownloadResult.DownloadResultState State,
        object? Tag)
    {
        public enum DownloadResultState
        {
            Remained,
            Overwritten,
            FreshNew,
            Broken
        }
    }
}