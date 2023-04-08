using CommunityToolkit.Mvvm.Messaging.Messages;
using Polymerium.Abstractions;

namespace Polymerium.App.Messages;

public class InstanceAddedMessage : ValueChangedMessage<GameInstance>
{
    public InstanceAddedMessage(GameInstance value) : base(value)
    {
    }
}