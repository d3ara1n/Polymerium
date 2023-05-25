using CommunityToolkit.Mvvm.Messaging.Messages;
using Polymerium.Abstractions;

namespace Polymerium.App.Messages;

public class InstanceRemovedMessage : ValueChangedMessage<GameInstance>
{
    public InstanceRemovedMessage(GameInstance value)
        : base(value) { }
}
