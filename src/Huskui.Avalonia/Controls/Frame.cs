using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_Container, typeof(TransitioningContentControl))]
public class Frame : ContentControl
{
    public delegate object? PageActivatorDelegate(Type page, object? parameter);

    public const string PART_Container = nameof(PART_Container);

    public static readonly DirectProperty<Frame, IPageTransition?> DefaultTransitionProperty =
        AvaloniaProperty.RegisterDirect<Frame, IPageTransition?>(nameof(DefaultTransition), o => o.DefaultTransition,
            (o, v) => o.DefaultTransition = v);

    public static readonly DirectProperty<Frame, bool> CanGoBackProperty =
        AvaloniaProperty.RegisterDirect<Frame, bool>(nameof(CanGoBack), o => o.CanGoBack,
            defaultBindingMode: BindingMode.OneWay);

    public static readonly DirectProperty<Frame, bool> CanGoBackOutOfStackProperty =
        AvaloniaProperty.RegisterDirect<Frame, bool>(nameof(CanGoBackOutOfStack), o => o.CanGoBackOutOfStack,
            (o, v) => o.CanGoBackOutOfStack = v);

    private bool _canGoBackOutOfStack;

    public bool CanGoBackOutOfStack
    {
        get => _canGoBackOutOfStack;
        set => SetAndRaise(CanGoBackOutOfStackProperty, ref _canGoBackOutOfStack, value);
    }


    private readonly InternalGoBackCommand _goBackCommand;

    private readonly Stack<FrameFrame> _history = new();

    private TransitioningContentControl? _container;

    private FrameFrame? _current;


    private IPageTransition? _defaultTransition = TransitioningContentControl.PageTransitionProperty.GetDefaultValue(
        typeof(TransitioningContentControl));

    public Frame()
    {
        _goBackCommand = new InternalGoBackCommand(this);
    }

    public IPageTransition? DefaultTransition
    {
        get => _defaultTransition;
        set => SetAndRaise(DefaultTransitionProperty, ref _defaultTransition, value);
    }

    public IEnumerable<FrameFrame> History => _history;
    public ICommand GoBackCommand => _goBackCommand;

    public bool CanGoBack => _history.Count > 0 || CanGoBackOutOfStack;

    public PageActivatorDelegate PageActivator { get; set; } =
        (t, _) => Activator.CreateInstance(t);

    public void Navigate(Type page, object? parameter = null, IPageTransition? transition = null)
    {
        Navigate(page, parameter, transition, false, true);
    }

    private void Navigate(Type page, object? parameter, IPageTransition? transition, bool reverse, bool stack)
    {
        var content = PageActivator(page, parameter) ?? throw new ArgumentNullException();
        ArgumentNullException.ThrowIfNull(_container);

        var old = CanGoBack;
        if (stack && _current is not null)
            _history.Push(_current);
        _current = new FrameFrame(page, parameter, transition);

        _container.PageTransition = transition ?? DefaultTransition;
        _container.IsTransitionReversed = reverse;
        Content = content;
        RaisePropertyChanged(CanGoBackProperty, old, CanGoBack);
        _goBackCommand.OnCanExecutedChanged();
    }

    public void GoBack()
    {
        ArgumentNullException.ThrowIfNull(_container);  
        if (_history.TryPop(out var frame))
            Navigate(frame.Page, frame.Parameter, frame.Transition, true, false);
        else if (CanGoBackOutOfStack)
        {
            Content = null;
            _current = null;
            _container.IsTransitionReversed = true;
        }
        else throw new InvalidOperationException("No previous page in the stack");

        RaisePropertyChanged(CanGoBackProperty, true, CanGoBack);
        _goBackCommand.OnCanExecutedChanged();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _container = e.NameScope.Find<TransitioningContentControl>(PART_Container);
    }

    public record FrameFrame(Type Page, object? Parameter, IPageTransition? Transition);

    private class InternalGoBackCommand(Frame host) : ICommand
    {
        public bool CanExecute(object? parameter)
        {
            return host.CanGoBack;
        }

        public void Execute(object? parameter)
        {
            host.GoBack();
        }

        public event EventHandler? CanExecuteChanged;

        internal void OnCanExecutedChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}