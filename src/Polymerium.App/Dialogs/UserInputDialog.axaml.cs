using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class UserInputDialog : Dialog
{
    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<UserInputDialog, string>(nameof(Watermark));

    public static readonly StyledProperty<string> PresetTextProperty =
        AvaloniaProperty.Register<UserInputDialog, string>(nameof(PresetText));

    public UserInputDialog() => InitializeComponent();

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public string PresetText
    {
        get => GetValue(PresetTextProperty);
        set => SetValue(PresetTextProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PresetTextProperty && change.NewValue is string preset)
        {
            Result = preset;
        }
    }


    protected override bool ValidateResult(object? result) => result is string str && !string.IsNullOrEmpty(str);
}