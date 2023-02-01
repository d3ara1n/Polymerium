using System;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;

namespace Polymerium.Abstractions;

public class GameInstance
{
    public string Id { get; set; }
    public GameMetadata Metadata { get; set; }
    public FileBasedLaunchConfiguration Configuration { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string FolderName { get; set; }
    public string ThumbnailFile { get; set; }
    public string BoundAccountId { get; set; }
    public DateTimeOffset? LastPlay { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastRestore { get; set; }
    public TimeSpan PlayTime { get; set; }
    public int PlayCount { get; set; }
    public int ExceptionCount { get; set; }
}