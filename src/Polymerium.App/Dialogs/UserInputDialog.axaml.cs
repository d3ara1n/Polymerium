using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Dialogs;

public partial class UserInputDialog : Dialog
{
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<UserInputDialog, string>(nameof(PlaceholderText));

    public static readonly StyledProperty<string> PresetTextProperty =
        AvaloniaProperty.Register<UserInputDialog, string>(nameof(PresetText));

    public static readonly StyledProperty<bool> AllowMultiLineProperty =
        AvaloniaProperty.Register<UserInputDialog, bool>(nameof(AllowMultiLine));

    public UserInputDialog() => InitializeComponent();


    public bool AllowMultiLine
    {
        get => GetValue(AllowMultiLineProperty);
        set => SetValue(AllowMultiLineProperty, value);
    }

    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
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
