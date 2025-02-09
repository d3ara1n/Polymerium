using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ToastHost, typeof(OverlayHost))]
[TemplatePart(PART_ModalHost, typeof(OverlayHost))]
[TemplatePart(PART_DialogHost, typeof(OverlayHost))]
[TemplatePart(PART_NotificationHost, typeof(NotificationHost))]
[PseudoClasses(":obstructed")]
public class AppWindow : Window
{
    public const string PART_ToastHost = nameof(PART_ToastHost);
    public const string PART_ModalHost = nameof(PART_ModalHost);
    public const string PART_DialogHost = nameof(PART_DialogHost);
    public const string PART_NotificationHost = nameof(PART_NotificationHost);

    public static readonly DirectProperty<AppWindow, bool> IsMaximizedProperty =
        AvaloniaProperty.RegisterDirect<AppWindow, bool>(nameof(IsMaximized), o => o.IsMaximized,
            (o, v) => o.IsMaximized = v);

    private OverlayHost? _dialogHost;

    private bool _isMaximized;
    private OverlayHost? _modalHost;

    private OverlayHost? _toastHost;
    private NotificationHost? _notificationHost;

    protected override Type StyleKeyOverride => typeof(AppWindow);

    public bool IsMaximized
    {
        get => _isMaximized;
        set => SetAndRaise(IsMaximizedProperty, ref _isMaximized, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty) IsMaximized = WindowState == WindowState.Maximized;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _notificationHost = e.NameScope.Find<NotificationHost>(PART_NotificationHost);
        _toastHost = e.NameScope.Find<OverlayHost>(PART_ToastHost);
        _modalHost = e.NameScope.Find<OverlayHost>(PART_ModalHost);
        _dialogHost = e.NameScope.Find<OverlayHost>(PART_DialogHost);
        _toastHost?.GetObservable(OverlayHost.IsPresentProperty).Subscribe(UpdateObstructed);
        _modalHost?.GetObservable(OverlayHost.IsPresentProperty).Subscribe(UpdateObstructed);
        _dialogHost?.GetObservable(OverlayHost.IsPresentProperty).Subscribe(UpdateObstructed);
    }

    private void UpdateObstructed(bool _)
    {
        ArgumentNullException.ThrowIfNull(_toastHost);
        ArgumentNullException.ThrowIfNull(_modalHost);
        ArgumentNullException.ThrowIfNull(_dialogHost);
        PseudoClasses.Set(":obstructed", _toastHost.IsPresent || _modalHost.IsPresent || _dialogHost.IsPresent);
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

    public void PopModal(Modal modal)
    {
        ArgumentNullException.ThrowIfNull(_modalHost);
        _modalHost.Pop(modal);
    }

    public void PopNotification(NotificationItem notification)
    {
        ArgumentNullException.ThrowIfNull(_notificationHost);
        _notificationHost.Pop(notification);
    }
}