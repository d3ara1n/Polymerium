using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Modals;

public partial class ProgressModal : Modal
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<ProgressModal, string>(nameof(Title));

    public static readonly StyledProperty<string> StatusTextProperty =
        AvaloniaProperty.Register<ProgressModal, string>(nameof(StatusText));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<ProgressModal, bool>(nameof(IsIndeterminate), true);

    public static readonly StyledProperty<int> ProgressValueProperty =
        AvaloniaProperty.Register<ProgressModal, int>(nameof(ProgressValue));

    public ProgressModal() => InitializeComponent();

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    public int ProgressValue
    {
        get => GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }
}
