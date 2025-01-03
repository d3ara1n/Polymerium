namespace Polymerium.Trident.Models.CurseForgeApi;

public record ObjectResponse<T>(T Data) where T : class;