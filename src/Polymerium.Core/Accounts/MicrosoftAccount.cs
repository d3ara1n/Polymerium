using Polymerium.Abstractions.Accounts;

namespace Polymerium.Core.Accounts;

public class MicrosoftAccount : IGameAccount
{
    public MicrosoftAccount(string id, string uuid, string nickname, string accessToken, string clientToken)
    {
        Id = id;
        UUID = uuid;
        Nickname = nickname;
        AccessToken = accessToken;
        ClientToken = clientToken;
    }

    private MicrosoftAccount()
    {
        Id = string.Empty;
        UUID = string.Empty;
        Nickname = string.Empty;
        AccessToken = string.Empty;
        ClientToken = string.Empty;
    }

    public string Id { get; set; }
    public string UUID { get; set; }
    public string Nickname { get; set; }
    public string AccessToken { get; set; }
    public string ClientToken { get; set; }

    public bool Validate()
    {
        return true;
    }

    public bool Refresh()
    {
        return true;
    }
}