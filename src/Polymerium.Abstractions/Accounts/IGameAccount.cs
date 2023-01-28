using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Abstractions.Accounts
{
    public interface IGameAccount
    {
        // 'login' method or something to get access

        // when created, generated an id
        string Id { get; set; }
        string UUID { get; set; }
        string Nickname { get; }
    }
}
