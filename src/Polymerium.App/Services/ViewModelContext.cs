using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

public class ViewModelContext : ObservableObject
{
    private GameInstanceModel associatedInstance;
    private AccountItemModel selectedAccount;

    public AccountItemModel SelectedAccount
    {
        get => selectedAccount;
        set => SetProperty(ref selectedAccount, value);
    }

    public GameInstanceModel AssociatedInstance
    {
        get => associatedInstance;
        set => SetProperty(ref associatedInstance, value);
    }
}