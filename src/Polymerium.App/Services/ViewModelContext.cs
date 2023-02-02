using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

public class ViewModelContext : ObservableObject
{
    private GameInstance associatedInstance;
    private AccountItemModel selectedAccount;

    public AccountItemModel SelectedAccount
    {
        get => selectedAccount;
        set => SetProperty(ref selectedAccount, value);
    }

    public GameInstance AssociatedInstance
    {
        get => associatedInstance;
        set
        {
            associatedInstance = value;
            OnPropertyChanged();
        }
    }
}