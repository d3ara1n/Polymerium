using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions.Accounts;

namespace Polymerium.Core.Accounts
{
    public class OfflineAccount : IGameAccount
    {
        public string Id { get; set; }
        public string UUID { get; set; }
        public string Nickname { get; set; }
    }
}
