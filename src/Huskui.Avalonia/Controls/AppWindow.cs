using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ContentPresenter, typeof(ContentControl))]
[TemplatePart(PART_ToastHost, typeof(OverlayHost))]
public class AppWindow : Window
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);
    public const string PART_ToastHost = nameof(PART_ToastHost);

    public static readonly DirectProperty<AppWindow, bool> IsMaximizedProperty =
        AvaloniaProperty.RegisterDirect<AppWindow, bool>(nameof(IsMaximized), o => o.IsMaximized,
            (o, v) => o.IsMaximized = v);

    private bool _isMaximized;

    protected override Type StyleKeyOverride => typeof(AppWindow);

    public bool IsMaximized
    {
        get => _isMaximized;
        set => SetAndRaise(IsMaximizedProperty, ref _isMaximized, value);
    }

    private OverlayHost? _toastHost;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty) IsMaximized = WindowState == WindowState.Maximized;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _toastHost = e.NameScope.Find<OverlayHost>(PART_ToastHost);
    }

    public void PopToast(Toast toast)
    {
        _toastHost?.Pop(toast);
    }
}