namespace Polymerium.Trident.Models.CurseForgeApi;

public record ArrayResponse<T>(IReadOnlyList<T> Data) where T : class;