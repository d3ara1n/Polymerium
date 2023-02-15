using Polymerium.Abstractions.Accounts;

namespace Polymerium.Core.Accounts;

public class MicrosoftAccount : IGameAccount
{
    public MicrosoftAccount(string id, string uuid, string nickname)
    {
        Id = id;
        UUID = uuid;
        Nickname = nickname;
    }

    private MicrosoftAccount()
    {
        Id = string.Empty;
        UUID = string.Empty;
        Nickname = string.Empty;
    }

    public string Id { get; set; }
    public string UUID { get; set; }
    public string Nickname { get; }
}