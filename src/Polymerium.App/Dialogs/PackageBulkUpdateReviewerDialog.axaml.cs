using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class PackageBulkUpdateReviewerDialog : Dialog
{
    public PackageBulkUpdateReviewerDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) => true;
}
