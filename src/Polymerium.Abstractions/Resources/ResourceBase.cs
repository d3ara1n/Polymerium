using System;

namespace Polymerium.Abstractions.Resources;

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

    // 通过 (domain@)type, id, versionId 三元组可以获得特定版本资源的文件三元组 fileName, hash, source
    // 这一过程通过进一步解析为 file 实现，这要求每一个 domain resolver 都支持 file type
    // 三元组不可以为空，因为所有资源必须对应到可下载的文件
    public string Id { get; set; }
    public string VersionId { get; set; }
    public Uri? Update { get; set; }
    public Uri? File { get; set; }
}
