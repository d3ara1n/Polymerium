namespace Polymerium.Trident.Models.Eternal;

public record ArrayResponse<T>(IReadOnlyList<T> Data) where T : class;