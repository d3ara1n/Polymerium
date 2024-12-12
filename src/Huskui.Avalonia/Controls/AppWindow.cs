using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Huskui.Avalonia.Controls;

[TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentControl))]
public class AppWindow : Window
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);

    public static readonly DirectProperty<AppWindow, bool> IsWindowMaximizedProperty =
        AvaloniaProperty.RegisterDirect<AppWindow, bool>(nameof(IsWindowMaximized), o => o.IsWindowMaximized,
            (o, v) => o.IsWindowMaximized = v);

    private bool _isWindowMaximized;

    protected override Type StyleKeyOverride => typeof(AppWindow);

    public bool IsWindowMaximized
    {
        get => _isWindowMaximized;
        set => SetAndRaise(IsWindowMaximizedProperty, ref _isWindowMaximized, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty) IsWindowMaximized = WindowState == WindowState.Maximized;
    }
}