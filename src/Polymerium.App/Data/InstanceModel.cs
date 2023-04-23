using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polymerium.Abstractions;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;
using Polymerium.Core;

namespace Polymerium.App.Data;

public class InstanceModel : RefinedModelBase<GameInstance>
{
    private static readonly Uri location = new(ConstPath.CONFIG_INSTANCE_FILE);

    private static readonly JsonSerializerSettings serializerSettings =
        new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };

    public override Uri Location => location;

    public override JsonSerializerSettings SerializerSettings => serializerSettings;

    public string? Id { get; set; }
    public GameMetadata? Metadata { get; set; }
    public FileBasedLaunchConfiguration? Configuration { get; set; }
    public Uri? ReferenceSource { get; set; }
    public string? Version { get; set; }
    public string? Name { get; set; }
    public string? Author { get; set; }
    public string? FolderName { get; set; }
    public Uri? ThumbnailFile { get; set; }
    public string? BoundAccountId { get; set; }
    public DateTimeOffset? LastPlay { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? LastRestore { get; set; }
    public TimeSpan? PlayTime { get; set; }
    public int? PlayCount { get; set; }
    public int? ExceptionCount { get; set; }

    public override void Apply(GameInstance instance)
    {
        Id = instance.Id;
        Metadata = instance.Metadata;
        Configuration = instance.Configuration;
        Name = instance.Name;
        Author = instance.Author;
        FolderName = instance.FolderName;
        ThumbnailFile = instance.ThumbnailFile;
        BoundAccountId = instance.BoundAccountId;
        LastPlay = instance.LastPlay;
        CreatedAt = instance.CreatedAt;
        LastRestore = instance.LastRestore;
        PlayTime = instance.PlayTime;
        PlayCount = instance.PlayCount;
        ExceptionCount = instance.ExceptionCount;
        ReferenceSource = instance.ReferenceSource;
        Version = instance.Version;
    }

    public override GameInstance Extract()
    {
        var res = new GameInstance(
            Id!,
            Metadata!.Value,
            Version!,
            ReferenceSource!,
            Configuration!,
            Name!,
            Author ?? string.Empty,
            FolderName!,
            ThumbnailFile,
            BoundAccountId ?? string.Empty,
            LastPlay!,
            CreatedAt ?? DateTimeOffset.Now,
            LastRestore!,
            PlayTime ?? TimeSpan.Zero,
            PlayCount ?? 0,
            ExceptionCount ?? 0
        );
        return res;
    }
}
