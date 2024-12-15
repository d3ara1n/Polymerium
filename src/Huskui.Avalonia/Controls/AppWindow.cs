using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ContentPresenter, typeof(ContentControl))]
[TemplatePart(PART_ToastHost, typeof(OverlayHost))]
[TemplatePart(PART_DialogHost, typeof(OverlayHost))]
public class AppWindow : Window
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);
    public const string PART_ToastHost = nameof(PART_ToastHost);
    public const string PART_DialogHost = nameof(PART_DialogHost);

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
    private OverlayHost? _dialogHost;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty) IsMaximized = WindowState == WindowState.Maximized;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _toastHost = e.NameScope.Find<OverlayHost>(PART_ToastHost);
        _dialogHost = e.NameScope.Find<OverlayHost>(PART_DialogHost);
    }

    public void PopToast(Toast toast)
    {
        ArgumentNullException.ThrowIfNull(_toastHost);
        _toastHost.Pop(toast);
    }

    public void PopDialog(Dialog dialog)
    {
        ArgumentNullException.ThrowIfNull(_dialogHost);
        _dialogHost.Pop(dialog);
    }
}