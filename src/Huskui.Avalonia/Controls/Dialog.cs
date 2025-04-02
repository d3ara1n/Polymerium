using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Huskui.Avalonia.Transitions;

namespace Huskui.Avalonia.Controls;

public class Dialog : HeaderedContentControl
{
    public static readonly StyledProperty<string> PrimaryTextProperty =
        AvaloniaProperty.Register<Dialog, string>(nameof(PrimaryText));

    public static readonly StyledProperty<string> SecondaryTextProperty =
        AvaloniaProperty.Register<Dialog, string>(nameof(SecondaryText));


    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<Dialog, string>(nameof(Message));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<Dialog, string>(nameof(Title));


    public static readonly DirectProperty<Dialog, OverlayHost?> HostProperty =
        AvaloniaProperty.RegisterDirect<Dialog, OverlayHost?>(nameof(Host), o => o.Host, (o, v) => o.Host = v);

    public static readonly DirectProperty<Dialog, OverlayItem?> ContainerProperty =
        AvaloniaProperty.RegisterDirect<Dialog, OverlayItem?>(nameof(Container),
                                                              o => o.Container,
                                                              (o, v) => o.Container = v);

    public static readonly DirectProperty<Dialog, object?> ResultProperty =
        AvaloniaProperty.RegisterDirect<Dialog, object?>(nameof(Result), o => o.Result, (o, v) => o.Result = v);

    public static readonly StyledProperty<bool> IsPrimaryButtonVisibleProperty =
        AvaloniaProperty.Register<Dialog, bool>(nameof(IsPrimaryButtonVisible));


    public readonly TaskCompletionSource<bool> CompletionSource = new();

    private OverlayItem? _container;
    private OverlayHost? _host;
    private object? _result;

    public Dialog()
    {
        PrimaryCommand = new InternalCommand(Confirm, CanConfirm);
        SecondaryCommand = new InternalCommand(Cancel);
    }

    public string PrimaryText
    {
        get => GetValue(PrimaryTextProperty);
        set => SetValue(PrimaryTextProperty, value);
    }

    public string SecondaryText
    {
        get => GetValue(SecondaryTextProperty);
        set => SetValue(SecondaryTextProperty, value);
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsPrimaryButtonVisible
    {
        get => GetValue(IsPrimaryButtonVisibleProperty);
        set => SetValue(IsPrimaryButtonVisibleProperty, value);
    }

    protected override Type StyleKeyOverride { get; } = typeof(Dialog);

    public object? Result
    {
        get => _result;
        set
        {
            SetAndRaise(ResultProperty, ref _result, value);
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