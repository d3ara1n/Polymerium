using Polymerium.Trident.Models.CurseForgeApi;
using Polymerium.Trident.Services;
using Refit;
using FileInfo = Polymerium.Trident.Models.CurseForgeApi.FileInfo;

namespace Polymerium.Trident.Clients;

public interface ICurseForgeClient
{
    [Get("/v1/minecraft/version")]
    Task<ArrayResponse<GameVersion>> GetMinecraftVersionsAsync(bool? sortDescending = null);

    [Get("/v1/mods/search")]
    Task<SearchResponse<ModInfo>> SearchModsAsync(
        string searchFilter,
        uint? classId,
        string? gameVersion,
        ModLoaderTypeModel? modLoaderType,
        string sortOrder = "desc",
        uint index = 0,
        uint pageSize = 50,
        uint gameId = CurseForgeService.GAME_ID);

    [Get("/v1/mods/{modId}")]
    Task<ObjectResponse<ModInfo>> GetModAsync(uint modId);

    [Get("/v1/mods/{modId}/files/{fileId}")]
    Task<ObjectResponse<FileInfo>> GetModFileAsync(uint modId, uint fileId);

    [Get("/v1/mods/{modId}/files")]
    Task<ArrayResponse<FileInfo>> GetModFilesAsync(
        uint modId,
        string? gameVersion,
        ModLoaderTypeModel? modLoaderType,
        uint? index,
        uint? pageSize);

    [Get("/v1/mods/{modId}/description")]
    Task<ObjectResponse<string>> GetModDescriptionAsync(uint modId);

    [Get("/v1/mods/{modId}/files/{fileId}/changelog")]
    Task<ObjectResponse<string>> GetModFileChangelogAsync(uint modId, uint fileId);
}