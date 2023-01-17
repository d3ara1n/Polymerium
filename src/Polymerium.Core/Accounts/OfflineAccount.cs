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
        public string Id => GeneratedId;
        public string DisplayName => PlayerName;
        // set by user
        public string PlayerName { get; set; }
        // set by logging process
        public string GeneratedId { get; set; }
    }
}
