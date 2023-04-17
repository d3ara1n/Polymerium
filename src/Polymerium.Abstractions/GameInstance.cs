using System;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;

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
        string? thumbnailFile,
        string? boundAccountId,
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
        LastPlay = lastPlay;
        CreatedAt = createdAt;
        LastRestore = lastRestore;
        PlayTime = playTime;
        PlayCount = playCount;
        ExceptionCount = exceptionCount;
    }

    public string Id { get; set; }
    public GameMetadata Metadata { get; set; }
    public string Version { get; set; }
    public Uri? ReferenceSource { get; set; }
    public FileBasedLaunchConfiguration Configuration { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string FolderName { get; set; }
    public string? ThumbnailFile { get; set; }
    public string? BoundAccountId { get; set; }
    public DateTimeOffset? LastPlay { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastRestore { get; set; }
    public TimeSpan PlayTime { get; set; }
    public int PlayCount { get; set; }
    public int ExceptionCount { get; set; }
}
