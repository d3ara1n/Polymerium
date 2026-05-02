using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Dialogs;

public partial class RuntimePickerDialog : Dialog
{
    public RuntimePickerDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) =>
        result is RuntimePickerDialogCandidateModel;
}
