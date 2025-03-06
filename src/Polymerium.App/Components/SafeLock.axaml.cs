using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Markup.Xaml;

namespace Polymerium.App.Components;

[PseudoClasses(":locked", ":unlocked")]
public partial class SafeLock : UserControl
{
    public static readonly StyledProperty<string> UserInputProperty =
        AvaloniaProperty.Register<SafeLock, string>(nameof(UserInput));

    public string UserInput
    {
        get => GetValue(UserInputProperty);
        set => SetValue(UserInputProperty, value);
    }

    public static readonly StyledProperty<string> SafeCodeProperty =
        AvaloniaProperty.Register<SafeLock, string>(nameof(SafeCode));

    public string SafeCode
    {
        get => GetValue(SafeCodeProperty);
        set => SetValue(SafeCodeProperty, value);
    }

    public static readonly DirectProperty<SafeLock, bool> IsUnlockedProperty =
        AvaloniaProperty.RegisterDirect<SafeLock, bool>(nameof(IsUnlocked), o => o.IsUnlocked);

    public bool IsUnlocked
    {
        get => field;
        private set => SetAndRaise(IsUnlockedProperty, ref field, value);
    }


    public SafeLock()
    {
        InitializeComponent();
        PseudoClasses.Set(":locked", true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsUnlockedProperty)
        {
            PseudoClasses.Set(":unlocked", change.GetNewValue<bool>());
            PseudoClasses.Set(":locked", !change.GetNewValue<bool>());
        }

        if (change.Property == UserInputProperty)
        {
            var input = change.GetNewValue<string>();
            IsUnlocked = input == SafeCode;
        }
    }
}