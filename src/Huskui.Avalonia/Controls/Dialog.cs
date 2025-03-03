using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Huskui.Avalonia.Transitions;

namespace Huskui.Avalonia.Controls;

public class Dialog : HeaderedContentControl
{
    public static readonly DirectProperty<Dialog, string> PrimaryTextProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(PrimaryText),
                                                        o => o.PrimaryText,
                                                        (o, v) => o.PrimaryText = v);

    public static readonly DirectProperty<Dialog, string> SecondaryTextProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(SecondaryText),
                                                        o => o.SecondaryText,
                                                        (o, v) => o.SecondaryText = v);

    public static readonly DirectProperty<Dialog, string> TitleProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(Title), o => o.Title, (o, v) => o.Title = v);

    public static readonly DirectProperty<Dialog, string> MessageProperty =
        AvaloniaProperty.RegisterDirect<Dialog, string>(nameof(Message), o => o.Message, (o, v) => o.Message = v);

    public static readonly DirectProperty<Dialog, OverlayHost?> HostProperty =
        AvaloniaProperty.RegisterDirect<Dialog, OverlayHost?>(nameof(Host), o => o.Host, (o, v) => o.Host = v);

    public static readonly DirectProperty<Dialog, OverlayItem?> ContainerProperty =
        AvaloniaProperty.RegisterDirect<Dialog, OverlayItem?>(nameof(Container),
                                                              o => o.Container,
                                                              (o, v) => o.Container = v);

    public static readonly DirectProperty<Dialog, object?> ResultProperty =
        AvaloniaProperty.RegisterDirect<Dialog, object?>(nameof(Result), o => o.Result, (o, v) => o.Result = v);

    public static readonly DirectProperty<Dialog, bool> IsPrimaryButtonVisibleProperty =
        AvaloniaProperty.RegisterDirect<Dialog, bool>(nameof(IsPrimaryButtonVisible),
                                                      o => o.IsPrimaryButtonVisible,
                                                      (o, v) => o.IsPrimaryButtonVisible = v);

    public readonly TaskCompletionSource<bool> CompletionSource = new();

    private OverlayItem? _container;
    private OverlayHost? _host;

    private bool _isPrimaryButtonVisible;

    private string _message = string.Empty;
    private string _primaryText = string.Empty;
    private object? _result;
    private string _secondaryText = string.Empty;
    private string _title = string.Empty;

    public Dialog()
    {
        PrimaryCommand = new InternalCommand(Confirm, CanConfirm);
        SecondaryCommand = new InternalCommand(Cancel);
    }

    protected override Type StyleKeyOverride { get; } = typeof(Dialog);

    public bool IsPrimaryButtonVisible
    {
        get => _isPrimaryButtonVisible;
        set => SetAndRaise(IsPrimaryButtonVisibleProperty, ref _isPrimaryButtonVisible, value);
    }


    public object? Result
    {
        get => _result;
        set
        {
            if (SetAndRaise(ResultProperty, ref _result, value))
                (PrimaryCommand as InternalCommand)?.OnCanExecuteChanged();
        }
    }


    public OverlayItem? Container
    {
        get => _container;
        set => SetAndRaise(ContainerProperty, ref _container, value);
    }


    public OverlayHost? Host
    {
        get => _host;
        set => SetAndRaise(HostProperty, ref _host, value);
    }

    public string PrimaryText
    {
        get => _primaryText;
        set => SetAndRaise(PrimaryTextProperty, ref _primaryText, value);
    }

    public string SecondaryText
    {
        get => _secondaryText;
        set => SetAndRaise(SecondaryTextProperty, ref _secondaryText, value);
    }

    public string Title
    {
        get => _title;
        set => SetAndRaise(TitleProperty, ref _title, value);
    }

    public string Message
    {
        get => _message;
        set => SetAndRaise(MessageProperty, ref _message, value);
    }


    public ICommand PrimaryCommand { get; }
    public ICommand SecondaryCommand { get; }

    protected virtual bool ValidateResult(object? result) => false;

    private bool CanConfirm() => ValidateResult(Result);

    private void Confirm()
    {
        if (Host != null && Container != null)
        {
            Container.Transition = new PopUpTransition();
            Host.Dismiss(Container);
        }

        CompletionSource.TrySetResult(CanConfirm());
    }

    private void Cancel()
    {
        if (Host != null && Container != null)
            Host.Dismiss(Container);
        CompletionSource.TrySetResult(false);
    }

    private class InternalCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => execute();
        public event EventHandler? CanExecuteChanged;

        internal void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}