using System;

namespace Polymerium.Abstractions.Resources;

// Uri 约定主体部分相同则判定为同一个资源，即 Uri 的主体部分相同则解析出来之后的结果中 Id 也相同，但 VersionId 可以不一样
// 该约定可以在解析之前就判断相同资源的不同版本
public abstract record ResourceBase
{
    protected ResourceBase(
        string id,
        string name,
        string version,
        string author,
        Uri? iconSource,
        Uri? reference,
        string summary,
        string versionId,
        Uri? update,
        Uri? file
    )
    {
        Id = id;
        Name = name;
        Version = version;
        Author = author;
        IconSource = iconSource;
        Reference = reference;
        Summary = summary;
        VersionId = versionId;
        Update = update;
        File = file;
    }

    public string Version { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public Uri? IconSource { get; set; }
    public Uri? Reference { get; set; }
    public string Summary { get; set; }
    public string Id { get; set; }
    public string VersionId { get; set; }
    public Uri? Update { get; set; }
    public Uri? File { get; set; }
}