namespace Trident.Abstractions;

public interface IAccount
{
    public string Username { get; }
    public string Uuid { get; }
    public string AccessToken { get; }
    public string UserType { get; }
    public ValueTask<bool> ValidateAsync(IHttpClientFactory factory, CancellationToken token = default);
    public ValueTask<bool> RefreshAsync(IHttpClientFactory factory, CancellationToken token = default);
}