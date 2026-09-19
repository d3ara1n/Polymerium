using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class ModpackExporterDialog : Dialog
{
    public ModpackExporterDialog()
    {
        InitializeComponent();
        ConfirmRequested += OnConfirmRequested;
    }

    public ModpackExporterModel? ConfirmedResult { get; private set; }

    protected override bool ValidateResult(object? result) =>
        result is ModpackExporterModel || ConfirmedResult is not null;

    private void OnConfirmRequested(object? sender, ConfirmRequestedEventArgs e)
    {
        // NOTE: 关闭时选择控件可能清空绑定，必须在卸载前保存本次确认的结果。
        ConfirmedResult = e.Result as ModpackExporterModel;
        e.Rejected = ConfirmedResult is null;
    }
}
