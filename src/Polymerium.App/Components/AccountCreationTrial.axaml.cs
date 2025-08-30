using System;
using Polymerium.App.Controls;
using Trident.Core.Accounts;

namespace Polymerium.App.Components;

public partial class AccountCreationTrial : AccountCreationStep
{
    public AccountCreationTrial() => InitializeComponent();

    public override object NextStep() =>
        new AccountCreationPreview
        {
            Account = RoleSelectBox.SelectedIndex switch
            {
                0 => TrialAccount.CreateStewie(),
                1 => TrialAccount.CreateBrian(),
                2 => TrialAccount.CreateChris(),
                3 => TrialAccount.CreatePeter(),
                4 => TrialAccount.CreateLois(),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
}
