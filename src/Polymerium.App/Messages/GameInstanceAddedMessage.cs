using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polymerium.Abstractions;

namespace Polymerium.App.Messages
{
    public class GameInstanceAddedMessage: CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<GameInstance>
    {
        public GameInstanceAddedMessage(GameInstance value) : base(value)
        {
            AddedInstance = value;
        }

        public GameInstance AddedInstance { get; set; }
    }
}
