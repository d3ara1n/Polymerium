using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class RuntimePickerDialog : Dialog
{
    public RuntimePickerDialog() => InitializeComponent();

    protected override bool ValidateResult(object? result) =>
        result is RuntimePickerDialogCandidateModel;
}
