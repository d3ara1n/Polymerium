using DotNext.Collections.Generic;
using Polymerium.Abstractions.ExtraData;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using System;
using System.Collections.Generic;

namespace Polymerium.Abstractions;

public class GameInstance
{
    public GameInstance(
        GameMetadata metadata,
        string version,
        FileBasedLaunchConfiguration configuration,
        string name,
        string folderName
    )
    {
        Id = Guid.NewGuid().ToString();
        Metadata = metadata;
        Version = version;
        Author = string.Empty;
        Configuration = configuration;
        Name = name;
        Note = string.Empty;
        Todos = new List<TodoItem>();
        FolderName = folderName;
        CreatedAt = DateTimeOffset.Now;
        PlayTime = TimeSpan.Zero;
    }

    public GameInstance(
        string id,
        GameMetadata metadata,
        string version,
        Uri? referenceSource,
        FileBasedLaunchConfiguration configuration,
        string name,
        string author,
        string folderName,
        Uri? thumbnailFile,
        string? boundAccountId,
        string note,
        IList<TodoItem> todos,
        DateTimeOffset? lastPlay,
        DateTimeOffset createdAt,
        DateTimeOffset? lastRestore,
        TimeSpan playTime,
        int playCount,
        int exceptionCount
    )
    {
        Id = id;
        Metadata = metadata;
        Version = version;
        ReferenceSource = referenceSource;
        Configuration = configuration;
        Name = name;
        Author = author;
        FolderName = folderName;
        ThumbnailFile = thumbnailFile;
        BoundAccountId = boundAccountId;
        Note = note;
        Todos = todos;
        LastPlay = lastPlay;
        CreatedAt = createdAt;
        LastRestore = lastRestore;
        PlayTime = playTime;
        PlayCount = playCount;
        ExceptionCount = exceptionCount;
    }

    public string Id { get; set; }
    public string Version { get; set; }
    public Uri? ReferenceSource { get; set; }
    public GameMetadata Metadata { get; set; }
    public FileBasedLaunchConfiguration Configuration { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string FolderName { get; set; }
    public Uri? ThumbnailFile { get; set; }
    public string? BoundAccountId { get; set; }

    #region 附加数据
    public string Note { get; set; }
    public IList<TodoItem> Todos { get; set; }
    #endregion

    #region 统计信息
    public DateTimeOffset? LastPlay { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastRestore { get; set; }
    public TimeSpan PlayTime { get; set; }
    public int PlayCount { get; set; }
    public int ExceptionCount { get; set; }
    #endregion
}
