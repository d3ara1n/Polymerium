namespace Polymerium.Trident.Models.CurseForgeApi;

public record GetVersionTypeResponse(uint Id, uint GameId, string Name, string Slug, bool IsSyncable, uint Status);