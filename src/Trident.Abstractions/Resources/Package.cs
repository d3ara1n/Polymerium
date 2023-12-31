﻿namespace Trident.Abstractions.Resources;

// 用于部署和用户界面展示
// 从 IRepository.Resolve(string projectId, string? versionId, Filter filter)

public record Package(string ProjectId, string ProjectName, string VersionId, string VersionName, Uri? Thumbnail, string Author,
    string Summary, Uri Reference, ResourceKind Kind, string FileName, Uri Download,
    string? Hash, Package.Requirement Requirements, IEnumerable<Dependency> Dependencies)
{

    public record Requirement(IEnumerable<string>? AnyOfVersions, IEnumerable<string>? AnyOfLoaders);
}