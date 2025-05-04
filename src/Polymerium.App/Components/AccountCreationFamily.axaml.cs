using System;
using Polymerium.App.Controls;
using Polymerium.Trident.Accounts;

namespace Polymerium.App.Components;

public partial class AccountCreationFamily : AccountCreationStep
{
    public AccountCreationFamily()
    {
        InitializeComponent();
    }

    public override object NextStep()
    {
        return new AccountCreationPreview
        {
            Account = RoleSelectBox.SelectedIndex switch
            {
                0 => FamilyAccount.CreateStewie(),
                1 => FamilyAccount.CreateBrian(),
                2 => FamilyAccount.CreateChris(),
                3 => FamilyAccount.CreatePeter(),
                4 => FamilyAccount.CreateLois(),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }
}