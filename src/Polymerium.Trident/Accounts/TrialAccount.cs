using Trident.Abstractions.Accounts;

namespace Polymerium.Trident.Accounts;

public class TrialAccount : IAccount
{
    public required string Username { get; init; }

    public required string Uuid { get; init; }

    public string AccessToken => "BirdIsTheAccessToken";

    public string UserType => "legacy";

    public ValueTask<bool> RefreshAsync(CancellationToken token) => ValueTask.FromResult(true);

    public ValueTask<bool> ValidateAsync(CancellationToken token) => ValueTask.FromResult(true);

    public static TrialAccount CreateStewie() => new() { Username = "Stewie", Uuid = Guid.NewGuid().ToString() };

    public static TrialAccount CreateBrian() => new() { Username = "Brian", Uuid = Guid.NewGuid().ToString() };

    public static TrialAccount CreateChris() => new() { Username = "Chris", Uuid = Guid.NewGuid().ToString() };

    public static TrialAccount CreatePeter() => new() { Username = "Peter", Uuid = Guid.NewGuid().ToString() };

    public static TrialAccount CreateLois() => new() { Username = "Lois", Uuid = Guid.NewGuid().ToString() };
}