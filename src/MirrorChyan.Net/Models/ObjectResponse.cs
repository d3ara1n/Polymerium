namespace MirrorChyan.Net.Models;

public record ObjectResponse<T>(int Code, string Msg, T Data) where T : class;
