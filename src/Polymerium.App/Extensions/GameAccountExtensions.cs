using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;

namespace Polymerium.App.Extensions
{
    public static class GameAccountExtensions
    {
        public static AccountItemModel ToModel(this IGameAccount account)
        {
            return new AccountItemModel()
            {
                Inner = account,
                AvatarBustSource = $"https://minotar.net/bust/{account.UUID}/100.png",
                AvatarFaceSource = $"https://minotar.net/helm/{account.UUID}/100.png"
            };
        }
    }
}
