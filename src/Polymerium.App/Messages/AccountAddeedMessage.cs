using CommunityToolkit.Mvvm.Messaging.Messages;
using Polymerium.Abstractions.Accounts;

namespace Polymerium.App.Messages
{
    public class AccountAddeedMessage : ValueChangedMessage<IGameAccount>
    {
        public IGameAccount LogonAccount { get; }

        public AccountAddeedMessage(IGameAccount value) : base(value)
        {
            LogonAccount = value;
        }
    }
}