using Trident.Abstractions;

namespace Polymerium.Trident.Services.Accounts;

public class AccountCollectionChangedEventArgs(AccountCollectionChangedAction action, IAccount account) : EventArgs
{
    public IAccount Account { get; } = account;
    public AccountCollectionChangedAction Action { get; } = action;
}