using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polymerium.Abstractions;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.Abstractions.Meta;

namespace Polymerium.App.Data;

public class InstanceModel : RefinedModelBase<GameInstance>
{
    private static readonly Uri location = new("poly-file:///instances.json", UriKind.Absolute);

    private static readonly JsonSerializerSettings serializerSettings = new()
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

    public string Id { get; set; }
    public GameMetadata Metadata { get; set; }
    public FileBasedLaunchConfiguration Configuration { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string FolderName { get; set; }
    public string ThumbnailFile { get; set; }
    public string BoundAccountId { get; set; }

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
    }

    public override GameInstance Extract()
    {
        var res = new GameInstance
        {
            Id = Id,
            Metadata = Metadata,
            Configuration = Configuration,
            Name = Name,
            Author = Author,
            FolderName = FolderName,
            ThumbnailFile = ThumbnailFile,
            BoundAccountId = BoundAccountId
        };
        return res;
    }
}