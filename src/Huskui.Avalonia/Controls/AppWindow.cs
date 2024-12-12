using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace Huskui.Avalonia.Controls;

[TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentControl))]
public class AppWindow : Window
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);

    public static readonly DirectProperty<AppWindow, bool> IsMaximizedProperty =
        AvaloniaProperty.RegisterDirect<AppWindow, bool>(nameof(IsMaximized), o => o.IsMaximized,
            (o, v) => o.IsMaximized = v);

    private bool isMaximized;

    protected override Type StyleKeyOverride => typeof(AppWindow);

    public bool IsMaximized
    {
        get => isMaximized;
        set => SetAndRaise(IsMaximizedProperty, ref isMaximized, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty) IsMaximized = WindowState == WindowState.Maximized;
    }
}