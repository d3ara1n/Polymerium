namespace Polymerium.Trident.Models.CurseForgeApi;

public record ArrayResponse<T>(IReadOnlyList<T> Data, Pagination Pagination) where T : class;