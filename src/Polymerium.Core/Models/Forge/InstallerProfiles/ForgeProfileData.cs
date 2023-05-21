using Newtonsoft.Json;

namespace Polymerium.Core.Models.Forge.InstallerProfiles;

public struct ForgeProfileData
{
    [JsonProperty("MAPPINGS")] public ForgeProfileDataItem? Mappings { get; set; }

    [JsonProperty("MOJMAPS")] public ForgeProfileDataItem? MojMaps { get; set; }

    [JsonProperty("MERGED_MAPPINGS")] public ForgeProfileDataItem? MergedMappings { get; set; }

    [JsonProperty("BINPATCH")] public ForgeProfileDataItem? BinPatched { get; set; }

    [JsonProperty("MC_UNPACKED")] public ForgeProfileDataItem? McUnpacked { get; set; }

    [JsonProperty("MC_SLIM")] public ForgeProfileDataItem? McSlim { get; set; }

    [JsonProperty("MC_SLIM_SHA")] public ForgeProfileDataItem? McSlimSha { get; set; }

    [JsonProperty("MC_EXTRA")] public ForgeProfileDataItem? McExtra { get; set; }

    [JsonProperty("MC_EXTRA_SHA")] public ForgeProfileDataItem? McExtraSha { get; set; }

    [JsonProperty("MC_SRG")] public ForgeProfileDataItem? McSrg { get; set; }

    [JsonProperty("PATCHED")] public ForgeProfileDataItem? Patched { get; set; }

    [JsonProperty("PATCHED_SHA")] public ForgeProfileDataItem? PatchedSha { get; set; }

    [JsonProperty("MCP_VERSION")] public ForgeProfileDataItem? McpVersion { get; set; }
}