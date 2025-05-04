using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Accounts;

public class OfflineAccount : IAccount
{
    public required string Username { get; init; }
    public required string Uuid { get; init; }
    public string AccessToken => "rand(32)";
    public string UserType => "legacy";
    public ValueTask<bool> ValidateAsync(CancellationToken token = default) => ValueTask.FromResult(true);

    public ValueTask<bool> RefreshAsync(CancellationToken token = default) => ValueTask.FromResult(true);
}