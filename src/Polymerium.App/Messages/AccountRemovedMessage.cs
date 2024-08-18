using Trident.Abstractions;

namespace Polymerium.App.Messages;

public class AccountRemovedMessage(IAccount account)
{
    public IAccount Account { get; } = account;
}