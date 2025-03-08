using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class UserInputDialog : Dialog
{
    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<UserInputDialog, string>(nameof(Watermark));

    public UserInputDialog()
    {
        InitializeComponent();
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }


    protected override bool ValidateResult(object? result) => result is string str && !string.IsNullOrEmpty(str);
}