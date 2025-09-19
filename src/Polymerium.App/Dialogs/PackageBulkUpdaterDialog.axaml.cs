using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class PackageBulkUpdaterDialog : Dialog
{
    public PackageBulkUpdaterDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) => true;
}
