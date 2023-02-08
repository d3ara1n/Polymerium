using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;

namespace Polymerium.App.Extensions;

public static class GameAccountExtensions
{
    public static AccountItemModel ToModel(this IGameAccount account)
    {
        return new AccountItemModel(account, $"https://minotar.net/bust/{account.UUID}/100.png",
            $"https://minotar.net/helm/{account.UUID}/100.png");
    }
}