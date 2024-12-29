namespace Polymerium.Trident.Models.Eternal;

public record GetVersionTypeResponse(uint Id, uint GameId, string Name, string Slug, bool IsSyncable, uint Status);