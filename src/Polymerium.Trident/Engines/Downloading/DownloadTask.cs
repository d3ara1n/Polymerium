namespace Polymerium.Trident.Engines.Downloading;

public record DownloadTask(string Target, Uri Source, string? Sha1, object? Tag);