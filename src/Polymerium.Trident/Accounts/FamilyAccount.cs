using Trident.Abstractions;

namespace Polymerium.Trident.Accounts;

public class FamilyAccount(string username, string uuid) : IAccount
{
    public string Username { get; } = username;

    public string Uuid { get; } = uuid;

    public string AccessToken => "invalid";

    public string UserType => "legacy";

    public ValueTask<bool> RefreshAsync(IHttpClientFactory factory, CancellationToken token) =>
        ValueTask.FromResult(true);

    public ValueTask<bool> ValidateAsync(IHttpClientFactory factory, CancellationToken token) =>
        ValueTask.FromResult(true);

    public static FamilyAccount CreateStewie() => new("Stewie", Guid.NewGuid().ToString());

    public static FamilyAccount CreateBrian() => new("Brian", Guid.NewGuid().ToString());

    public static FamilyAccount CreatePeter() => new("Peter", Guid.NewGuid().ToString());

    public static FamilyAccount CreateLois() => new("Lois", Guid.NewGuid().ToString());
}