namespace Polymerium.Trident.Models.XboxLiveApi;

public record XboxLiveRequest<T>(T Properties, string RelyingParty, string TokenType = "JWT") where T : class;