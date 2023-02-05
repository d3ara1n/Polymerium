using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Configurations;

namespace Polymerium.App.Data;

public class ConfigurationModel : RefinedModelBase<Configuration>
{
    public override Uri Location { get; } = new("poly-file:///configuration.json");

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

    public override JsonSerializerSettings SerializerSettings => serializerSettings;
    public AppSettings Settings { get; set; }
    public string AccountShowcaseId { get; set; }

    public FileBasedLaunchConfiguration GameGlobals { get; set; }

    public override void Apply(Configuration data)
    {
        Settings = data.Settings;
        AccountShowcaseId = data.AccountShowcaseId;
        GameGlobals = data.GameGlobals;
    }

    public override Configuration Extract()
    {
        var cfg = new Configuration
        {
            Settings = Settings,
            AccountShowcaseId = AccountShowcaseId,
            GameGlobals = GameGlobals
        };
        return cfg;
    }
}