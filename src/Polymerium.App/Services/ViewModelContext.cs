using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.Abstractions.Accounts;
using Polymerium.App.Models;

namespace Polymerium.App.Services;

public class ViewModelContext : ObservableObject
{
    private GameInstanceModel? associatedInstance;
    private IGameAccount? selectedAccount;
    private IGameAccount? accountShowcase;

    private readonly ConfigurationManager _configurationManager;
    private readonly AccountManager _accountManager;

    public ViewModelContext(
        ConfigurationManager configurationManager,
        AccountManager accountManager
    )
    {
        _configurationManager = configurationManager;
        _accountManager = accountManager;
        if (
            _accountManager.TryFindById(
                _configurationManager.Current.AccountShowcaseId ?? string.Empty,
                out var account
            )
        )
            accountShowcase = account!;
    }

    public IGameAccount? SelectedAccount
    {
        get => associatedInstance != null ? selectedAccount : accountShowcase;
        set
        {
            if (associatedInstance != null)
                associatedInstance.BoundAccountId = value?.Id;
            else
                AccountShowcase = value;

            SetProperty(ref selectedAccount, value);
        }
    }

    public IGameAccount? AccountShowcase
    {
        get => accountShowcase;
        set
        {
            _configurationManager.Current.AccountShowcaseId = value?.Id;
            SetProperty(ref accountShowcase, value);
        }
    }

    public GameInstanceModel? AssociatedInstance
    {
        get => associatedInstance;
        set
        {
            SetProperty(ref associatedInstance, value);
            if (value == null)
            {
                SelectedAccount = AccountShowcase;
            }
            else
            {
                _accountManager.TryFindById(value.BoundAccountId!, out var account);
                SelectedAccount = account;
            }
        }
    }
}
