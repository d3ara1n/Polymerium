using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Dialogs;

public partial class PackageBulkUpdateReviewerDialog : Dialog
{
    public PackageBulkUpdateReviewerDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) => true;
}
