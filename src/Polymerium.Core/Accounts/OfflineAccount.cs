using Polymerium.Abstractions.Accounts;

namespace Polymerium.Core.Accounts;

public class OfflineAccount : IGameAccount
{
    public OfflineAccount(string id, string uuid, string accessToken, string nickname)
    {
        Id = id;
        UUID = uuid;
        AccessToken = accessToken;
        Nickname = nickname;
    }

    private OfflineAccount()
    {
        Id = string.Empty;
        UUID = string.Empty;
        Nickname = string.Empty;
        AccessToken = string.Empty;
    }

    public string Id { get; set; }
    public string UUID { get; set; }
    public string Nickname { get; set; }
    public string AccessToken { get; set; }

    public bool Validate()
    {
        return true;
    }

    public bool Refresh()
    {
        return true;
    }
}