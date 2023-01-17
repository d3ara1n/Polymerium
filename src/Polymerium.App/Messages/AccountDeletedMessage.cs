using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Polymerium.Abstractions.Accounts;

namespace Polymerium.App.Messages
{
    public class AccountDeletedMessage : ValueChangedMessage<IGameAccount>
    {
        public IGameAccount DeletedAccount { get; }
        public AccountDeletedMessage(IGameAccount value) : base(value)
        {
            DeletedAccount = value;
        }
    }
}
