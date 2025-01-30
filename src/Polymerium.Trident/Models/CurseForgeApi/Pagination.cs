namespace Polymerium.Trident.Models.CurseForgeApi;

public record Pagination(uint Index, uint PageSize, uint ResultCount, ulong TotalCount);