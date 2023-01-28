using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Messages;
using Polymerium.App.Models;
using Windows.Gaming.UI;

namespace Polymerium.App.Services
{
    // Shared Objects service
    public class MemoryStorage : ObservableObject
    {
        public ObservableCollection<GameInstance> Instances { get; }
        public ObservableCollection<IGameAccount> Accounts { get; }
        private AccountItemModel selectedAccount;
        public AccountItemModel SelectedAccount { get => selectedAccount; set => SetProperty(ref selectedAccount, value); }
        public MemoryStorage()
        {
            Instances = new ObservableCollection<GameInstance>();
            Accounts = new ObservableCollection<IGameAccount>();
        }
    }
}
