using System;
using Avalonia;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Trident.Abstractions.Accounts;

namespace Polymerium.App.Components;

public partial class AccountCreationPreview : AccountCreationStep
{
    public static readonly DirectProperty<AccountCreationPreview, IAccount?> AccountProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationPreview, IAccount?>(nameof(Account),
                                                                           o => o.Account,
                                                                           (o, v) => o.Account = v);

    public IAccount? Account
    {
        get;
        set => SetAndRaise(AccountProperty, ref field, value);
    }

    public static readonly DirectProperty<AccountCreationPreview, AccountModel?> ModelProperty =
        AvaloniaProperty.RegisterDirect<AccountCreationPreview, AccountModel?>(nameof(Model),
                                                                               o => o.Model,
                                                                               (o, v) => o.Model = v);

    public AccountModel? Model
    {
        get;
        set => SetAndRaise(ModelProperty, ref field, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AccountProperty)
        {
            var account = change.GetNewValue<IAccount>();
            Model = new AccountModel(account.GetType(), account.Uuid, account.Username, DateTimeOffset.Now, null);
        }
    }


    public AccountCreationPreview() => InitializeComponent();

    public override object NextStep() => throw new NotImplementedException();
}