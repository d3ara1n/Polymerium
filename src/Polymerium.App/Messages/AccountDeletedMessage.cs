using CommunityToolkit.Mvvm.Messaging.Messages;
using Polymerium.Abstractions.Accounts;

namespace Polymerium.App.Messages;

public class AccountDeletedMessage : ValueChangedMessage<IGameAccount>
{
    public AccountDeletedMessage(IGameAccount value) : base(value)
    {
        DeletedAccount = value;
    }

    public IGameAccount DeletedAccount { get; }
}