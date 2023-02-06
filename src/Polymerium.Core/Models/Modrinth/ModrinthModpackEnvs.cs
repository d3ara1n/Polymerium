namespace Polymerium.Core.Models.Modrinth;

public enum ModrinthModpackEnv
{
    Required,
    Optional,
    Unsupported
}

public struct ModrinthModpackEnvs
{
    public ModrinthModpackEnv Client { get; set; }
    public ModrinthModpackEnv Server { get; set; }
}