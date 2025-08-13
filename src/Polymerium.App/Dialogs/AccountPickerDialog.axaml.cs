using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs
{
    public partial class AccountPickerDialog : Dialog
    {
        public static readonly DirectProperty<AccountPickerDialog, IReadOnlyList<AccountModel>> AccountsSourceProperty =
            AvaloniaProperty.RegisterDirect<AccountPickerDialog, IReadOnlyList<AccountModel>>(nameof(AccountsSource),
                o => o.AccountsSource,
                (o, v) => o.AccountsSource = v);

        public static readonly DirectProperty<AccountPickerDialog, ICommand> GotoManagerViewCommandProperty =
            AvaloniaProperty.RegisterDirect<AccountPickerDialog, ICommand>(nameof(GotoManagerViewCommand),
                                                                           o => o.GotoManagerViewCommand,
                                                                           (o, v) => o.GotoManagerViewCommand = v);

        public AccountPickerDialog() => InitializeComponent();

        public required IReadOnlyList<AccountModel> AccountsSource
        {
            get;
            set => SetAndRaise(AccountsSourceProperty, ref field, value);
        }

        public required ICommand GotoManagerViewCommand
        {
            get;
            set => SetAndRaise(GotoManagerViewCommandProperty, ref field, value);
        }


        protected override bool ValidateResult(object? result) => result is AccountModel;
    }
}
