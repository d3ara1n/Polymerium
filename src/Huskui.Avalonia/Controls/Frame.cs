using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ContentPresenter, typeof(ContentPresenter))]
[TemplatePart(PART_ContentPresenter2, typeof(ContentPresenter))]
public class Frame : TemplatedControl
{
    public delegate object? PageActivatorDelegate(Type page, object? parameter);

    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);
    public const string PART_ContentPresenter2 = nameof(PART_ContentPresenter2);

    public static readonly DirectProperty<Frame, IPageTransition> DefaultTransitionProperty =
        AvaloniaProperty.RegisterDirect<Frame, IPageTransition>(nameof(DefaultTransition), o => o.DefaultTransition,
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

    public static readonly DirectProperty<Frame, object?> ContentProperty =
        AvaloniaProperty.RegisterDirect<Frame, object?>(nameof(Content), o => o.Content, (o, v) => o.Content = v);

    private object? _content;

    public object? Content
    {
        get => _content;
        set => SetAndRaise(ContentProperty, ref _content, value);
    }

    private readonly InternalGoBackCommand _goBackCommand;

    private readonly Stack<FrameFrame> _history = new();

    private TransitioningContentControl? _container;
    private ContentPresenter? _presenter;
    private ContentPresenter? _presenter2;

    private FrameFrame? _currentFrame;
    private CancellationTokenSource? _currentToken;
    private (object? Content, IPageTransition Transition, bool Reverse)? _current;
    private bool _doubleArrangeSafeLock;


    private IPageTransition _defaultTransition = TransitioningContentControl.PageTransitionProperty.GetDefaultValue(
        typeof(TransitioningContentControl)) ?? new CrossFade(TimeSpan.FromMilliseconds(197));

    public Frame()
    {
        _goBackCommand = new InternalGoBackCommand(this);
    }

    public IPageTransition DefaultTransition
    {
        get => _defaultTransition;
        set => SetAndRaise(DefaultTransitionProperty, ref _defaultTransition, value);
    }

    public IEnumerable<FrameFrame> History => _history;
    public ICommand GoBackCommand => _goBackCommand;

    public bool CanGoBack => _history.Count > 0 || CanGoBackOutOfStack;

    public PageActivatorDelegate PageActivator { get; set; } =
        (t, _) => Activator.CreateInstance(t);

    public void Navigate(Type page, object? parameter, IPageTransition? transition)
    {
        ArgumentNullException.ThrowIfNull(_presenter);
        ArgumentNullException.ThrowIfNull(_presenter2);
        var content = PageActivator(page, parameter) ?? throw new ArgumentNullException();
        var old = CanGoBack;
        if (_currentFrame is not null)
            _history.Push(_currentFrame);
        _currentFrame = new FrameFrame(page, parameter, transition);

        UpdateContent(content, transition ?? DefaultTransition, false);

        RaisePropertyChanged(CanGoBackProperty, old, CanGoBack);
        _goBackCommand.OnCanExecutedChanged();
    }

    public void GoBack()
    {
        ArgumentNullException.ThrowIfNull(_presenter);
        ArgumentNullException.ThrowIfNull(_presenter2);
        if (_history.TryPop(out var frame))
        {
            var content = PageActivator(frame.Page, frame.Parameter) ?? throw new ArgumentNullException();
            UpdateContent(content, _currentFrame?.Transition ?? DefaultTransition, true);
            _currentFrame = frame;
        }
        else if (CanGoBackOutOfStack)
        {
            _currentFrame = null;
            UpdateContent(null, _currentFrame?.Transition ?? DefaultTransition, true);
        }
        else throw new InvalidOperationException("No previous page in the stack");

        RaisePropertyChanged(CanGoBackProperty, true, CanGoBack);
        _goBackCommand.OnCanExecutedChanged();
    }

    private void UpdateContent(object? content, IPageTransition transition, bool reverse)
    {
        Content = content;
        _current = new ValueTuple<object, IPageTransition, bool>(content, transition, reverse);
        _doubleArrangeSafeLock = true;
        InvalidateArrange();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rv = base.ArrangeOverride(finalSize);

        ArgumentNullException.ThrowIfNull(_presenter);
        ArgumentNullException.ThrowIfNull(_presenter2);

        if (_current.HasValue && _doubleArrangeSafeLock)
        {
            _currentToken?.Cancel();
            _doubleArrangeSafeLock = false;
            var cancel = new CancellationTokenSource();
            _currentToken = cancel;

            var (from, to) = _presenter.Content is not null ? (_presenter, _presenter2) : (_presenter2, _presenter);


            (from.ZIndex, to.ZIndex) = (0, 1);
            (from.IsVisible, to.IsVisible) = (true, true);
            to.Content = _current.Value.Content;

            _current.Value.Transition.Start(from, to, !_current.Value.Reverse, cancel.Token)
                .ContinueWith(_ =>
                {
                    if (!cancel.IsCancellationRequested)
                    {
                        from.Content = null;
                        (from.IsVisible, to.IsVisible) = (false, true);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        return rv;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _presenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
        _presenter2 = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter2);
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