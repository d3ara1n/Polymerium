using Polymerium.Abstractions.Accounts;

namespace Polymerium.Core.Accounts;

public class OfflineAccount : IGameAccount
{
    public OfflineAccount(string id, string uuid, string nickname)
    {
        Id = id;
        UUID = uuid;
        Nickname = nickname;
    }

    private OfflineAccount()
    {
        Id = string.Empty;
        UUID = string.Empty;
        Nickname = string.Empty;
    }

    public string Id { get; set; }
    public string UUID { get; set; }
    public string Nickname { get; set; }
}