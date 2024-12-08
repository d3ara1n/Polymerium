﻿namespace Trident.Abstractions.Repositories.Resources;

public record Exhibit(
    string Label,
    string Namespace,
    string Pid,
    string Name,
    Uri? Thumbnail,
    string Author,
    string Summary,
    uint DownloadCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);