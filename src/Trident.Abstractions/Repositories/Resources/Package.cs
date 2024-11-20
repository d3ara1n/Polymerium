﻿namespace Trident.Abstractions.Repositories.Resources;

public record Package(
    string Label,
    string Namespace,
    string ProjectId,
    string VersionId,
    string ProjectName,
    string VersionName,
    Uri? Thumbnail,
    string Author,
    string Summary,
    Uri Reference,
    ResourceKind Kind,
    ReleaseType ReleaseType,
    DateTimeOffset PublishedAt,
    Uri Download,
    string FileName,
    string? Hash,
    Requirement Requirements,
    IEnumerable<Dependency> Dependencies
);