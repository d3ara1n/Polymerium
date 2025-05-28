using Polymerium.Trident.Models.ModrinthApi;
using Refit;

namespace Polymerium.Trident.Clients;

public interface IModrinthClient
{
    [Get("/v2/tag/game_version")]
    Task<IReadOnlyList<GameVersion>> GetGameVersionsAsync();

    [Get("/v3/tag/loader")]
    Task<IReadOnlyList<ModLoader>> GetLoadersAsync();

    [Get("/v2/tag/project_type")]
    Task<IReadOnlyList<string>> GetProjectTypesAsync();

    [Get("/v2/search")]
    Task<SearchResponse<SearchHit>> SearchAsync(
        string query,
        string facets,
        string? index = null,
        uint offset = 0,
        uint limit = 10);

    [Get("/v3/project/{projectId}")]
    Task<ProjectInfo> GetProjectAsync(string projectId);

    [Get("/v3/version/{versionId}")]
    Task<VersionInfo> GetVersionAsync(string versionId);

    [Get("/v3/team/{teamId}/members")]
    Task<IReadOnlyList<MemberInfo>> GetTeamMembersAsync(string teamId);

    [Get("/v3/project/{projectId}/version")]
    Task<IReadOnlyList<VersionInfo>> GetProjectVersionsAsync(
        string projectId,
        string? loaders = null,
        string? gameVersions = null);
}