using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Polymerium.Abstractions;
using Polymerium.App.Messages;
using Windows.Gaming.UI;

namespace Polymerium.App.Services
{
    // 用于剥离依赖，避免循环引用。顺便把 messaging 的活干了
    public class MemoryStorage
    {
        public ObservableCollection<GameInstance> Instances { get; }
        public MemoryStorage()
        {
            Instances = new ObservableCollection<GameInstance>();
        }
    }
}
