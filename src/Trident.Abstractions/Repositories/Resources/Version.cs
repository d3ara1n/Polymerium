﻿namespace Trident.Abstractions.Repositories.Resources;

public record Version(
    string Label,
    string Namespace,
    string ProjectId,
    string VersionId,
    string VersionName,
    ReleaseType ReleaseType,
    DateTimeOffset PublishedAt,
    uint DownloadCount,
    string Changelog
);