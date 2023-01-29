using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;
using System.Collections.ObjectModel;

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