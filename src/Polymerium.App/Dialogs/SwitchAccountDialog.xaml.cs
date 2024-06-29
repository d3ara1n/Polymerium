// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Xaml;
using Polymerium.App.Models;
using System.Collections.Generic;

namespace Polymerium.App.Dialogs
{
    public sealed partial class SwitchAccountDialog
    {
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            nameof(Result), typeof(AccountModel), typeof(SwitchAccountDialog),
            new PropertyMetadata(null));

        public SwitchAccountDialog(XamlRoot xamlRoot, IList<AccountModel> candidates)
        {
            XamlRoot = xamlRoot;
            Candidates = candidates;

            InitializeComponent();
        }

        public IList<AccountModel> Candidates { get; }

        public AccountModel? Result
        {
            get => (AccountModel?)GetValue(ResultProperty);
            set => SetValue(ResultProperty, value);
        }
    }
}