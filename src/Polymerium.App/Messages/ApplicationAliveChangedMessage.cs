using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Polymerium.App.Messages
{
    public class ApplicationAliveChangedMessage : ValueChangedMessage<bool>
    {
        public ApplicationAliveChangedMessage(bool value) : base(value)
        {
            // false: nothing but the application is DYING!
        }
    }
}
