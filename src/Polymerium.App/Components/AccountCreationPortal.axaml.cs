using System;
using Avalonia;
using Polymerium.App.Controls;

namespace Polymerium.App.Components;

public partial class AccountCreationPortal : AccountCreationStep
{
    public static readonly DirectProperty<AccountCreationPortal, bool> IsOfflineAvailableProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationPortal, bool>(nameof(IsOfflineAvailable),
                                                                     o => o.IsOfflineAvailable,
                                                                     (o, v) => o.IsOfflineAvailable = v);

    public bool IsOfflineAvailable
    {
        get;
        set => SetAndRaise(IsOfflineAvailableProperty, ref field, value);
    }


    public AccountCreationPortal()
    {
        InitializeComponent();
    }

    public override object NextStep()
    {
        return AccountTypeSelectBox.SelectedIndex switch
        {
            0 => new AccountCreationMicrosoft(),
            1 => new AccountCreationFamily(),
            2 => new AccountCreationOffline(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}