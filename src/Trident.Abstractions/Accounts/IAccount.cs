namespace Trident.Abstractions.Accounts;

public interface IAccount
{
    string Username { get; }
    string Uuid { get; }
    string AccessToken { get; }
    string UserType { get; }
    ValueTask<bool> ValidateAsync(CancellationToken token = default);
    ValueTask<bool> RefreshAsync(CancellationToken token = default);
}