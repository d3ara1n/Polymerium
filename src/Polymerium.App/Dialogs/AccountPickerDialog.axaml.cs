using System.Collections.Generic;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class AccountPickerDialog : Dialog
{
    public AccountPickerDialog() => InitializeComponent();

    public static readonly DirectProperty<AccountPickerDialog, IReadOnlyList<AccountModel>> AccountsSourceProperty =
        AvaloniaProperty.RegisterDirect<AccountPickerDialog, IReadOnlyList<AccountModel>>(nameof(AccountsSource),
            o => o.AccountsSource,
            (o, v) => o.AccountsSource = v);

    public required IReadOnlyList<AccountModel> AccountsSource
    {
        get;
        set => SetAndRaise(AccountsSourceProperty, ref field, value);
    }

    protected override bool ValidateResult(object? result) => result is AccountModel;
}