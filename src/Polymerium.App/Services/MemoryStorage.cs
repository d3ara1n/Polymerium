using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;
using Polymerium.Core.Components;

namespace Polymerium.App.Services;

// Shared Objects service
public class MemoryStorage : ObservableObject
{
    private AccountItemModel selectedAccount;

    public MemoryStorage()
    {
        Instances = new ObservableCollection<GameInstance>();
        Accounts = new ObservableCollection<IGameAccount>();
    }

    public ObservableCollection<GameInstance> Instances { get; }
    public ObservableCollection<IGameAccount> Accounts { get; }

    public AccountItemModel SelectedAccount
    {
        get => selectedAccount;
        set => SetProperty(ref selectedAccount, value);
    }

    public IEnumerable<ComponentMeta> SupportedComponents { get; set; }
}