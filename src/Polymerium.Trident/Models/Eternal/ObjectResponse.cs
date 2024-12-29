namespace Polymerium.Trident.Models.Eternal;

public record ObjectResponse<T>(T Data) where T : class;