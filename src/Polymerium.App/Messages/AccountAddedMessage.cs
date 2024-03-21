using Trident.Abstractions;

namespace Polymerium.App.Messages
{
    public class AccountAddedMessage(IAccount account)
    {
        public IAccount Account { get; } = account;
    }
}