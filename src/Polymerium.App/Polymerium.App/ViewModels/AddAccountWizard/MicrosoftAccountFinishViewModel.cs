using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Services;
using Polymerium.Core.Accounts;

namespace Polymerium.App.ViewModels.AddAccountWizard;

public class MicrosoftAccountFinishViewModel : ObservableObject
{
    private readonly AccountManager _accountManager;

    public MicrosoftAccountFinishViewModel(AccountManager accountManager)
    {
        _accountManager = accountManager;
    }

    public void AddAccount(MicrosoftAccount account)
    {
        _accountManager.AddAccount(account);
    }
}