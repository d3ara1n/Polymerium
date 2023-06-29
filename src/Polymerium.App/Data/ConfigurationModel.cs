using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polymerium.Abstractions.LaunchConfigurations;
using Polymerium.App.Configurations;
using Polymerium.Core;
using System;

namespace Polymerium.App.Data;

public class ConfigurationModel : RefinedModelBase<Configuration>
{
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

    public override Uri Location { get; } = new(ConstPath.CONFIG_CONFIGURATION_FILE);

    public override JsonSerializerSettings SerializerSettings => serializerSettings;
    public string? AccountShowcaseId { get; set; }

    public FileBasedLaunchConfiguration? GameGlobals { get; set; }

    public override void Apply(Configuration data)
    {
        AccountShowcaseId = data.AccountShowcaseId;
        GameGlobals = data.GameGlobals;
    }

    public override Configuration Extract()
    {
        var cfg = new Configuration(
            AccountShowcaseId ?? string.Empty,
            GameGlobals ?? new FileBasedLaunchConfiguration()
        );
        return cfg;
    }
}
