using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Polymerium.Abstractions.DownloadSources.Models;

namespace Polymerium.App.Messages
{
    public class GameInstanceDeletedMessage : ValueChangedMessage<string>
    {
        public string DeletedInstanceId {get;set;}
        public GameInstanceDeletedMessage(string value) : base(value)
        {
            DeletedInstanceId = value;
        }
    }
}
