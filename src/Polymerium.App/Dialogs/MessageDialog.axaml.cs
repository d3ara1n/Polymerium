using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs
{
    public partial class MessageDialog : Dialog
    {
        public MessageDialog() => InitializeComponent();

        protected override bool ValidateResult(object? result) => true;
    }
}
