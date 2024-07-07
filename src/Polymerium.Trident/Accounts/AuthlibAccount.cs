using Trident.Abstractions;

namespace Polymerium.Trident.Accounts;

public class AuthlibAccount(string server, string identifier, string username, string accessToken, string uuid)
    : IAccount
{
    public string Server { get; } = server;
    public string Identifier { get; } = identifier;
    public string Username { get; } = username;
    public string AccessToken { get; } = accessToken;
    public string Uuid { get; } = uuid;
    public string UserType => "mojang";

    public ValueTask<bool> RefreshAsync(IHttpClientFactory factory, CancellationToken token) =>
        throw new NotImplementedException();

    public ValueTask<bool> ValidateAsync(IHttpClientFactory factory, CancellationToken token) =>
        throw new NotImplementedException();
}