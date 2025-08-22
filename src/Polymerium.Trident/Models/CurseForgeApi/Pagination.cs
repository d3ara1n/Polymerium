namespace Polymerium.Trident.Models.CurseForgeApi;

public readonly record struct Pagination(uint Index, uint PageSize, uint ResultCount, uint TotalCount);