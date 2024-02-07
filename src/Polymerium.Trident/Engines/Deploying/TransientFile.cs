namespace Polymerium.Trident.Engines.Deploying;

public record TransientFile(string SourcePath, string? TargetPath, Uri? Url, string? Sha1);