using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.LogicalTree;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ContentPresenter, typeof(ContentPresenter))]
[TemplatePart(PART_ContentPresenter2, typeof(ContentPresenter))]
public class Frame : TemplatedControl
{
    #region Delegates

    public delegate object? PageActivatorDelegate(Type page, object? parameter);

    #endregion

    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);
    public const string PART_ContentPresenter2 = nameof(PART_ContentPresenter2);

    public static readonly DirectProperty<Frame, IPageTransition> DefaultTransitionProperty =
        AvaloniaProperty.RegisterDirect<Frame, IPageTransition>(nameof(DefaultTransition),
                                                                o => o.DefaultTransition,
                                                                (o, v) => o.DefaultTransition = v);

    public static readonly DirectProperty<Frame, bool> CanGoBackProperty =
        AvaloniaProperty.RegisterDirect<Frame, bool>(nameof(CanGoBack),
                                                     o => o.CanGoBack,
                                                     defaultBindingMode: BindingMode.OneWay);

    public static readonly DirectProperty<Frame, bool> CanGoBackOutOfStackProperty =
        AvaloniaProperty.RegisterDirect<Frame, bool>(nameof(CanGoBackOutOfStack),
                                                     o => o.CanGoBackOutOfStack,
                                                     (o, v) => o.CanGoBackOutOfStack = v);

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<Frame, object?>(nameof(Content));


    private readonly InternalGoBackCommand _goBackCommand;

    private readonly Stack<FrameFrame> _history = new();

    private (object? Content, IPageTransition Transition, bool Reverse)? _current;

    private FrameFrame? _currentFrame;
    private CancellationTokenSource? _currentToken;


    private bool _doubleArrangeSafeLock;
    private ContentPresenter? _presenter;
    private ContentPresenter? _presenter2;

    public Frame() => _goBackCommand = new InternalGoBackCommand(this);

    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public bool CanGoBackOutOfStack
    {
        get;
        set => SetAndRaise(CanGoBackOutOfStackProperty, ref field, value);
    }

    public IPageTransition DefaultTransition
    {
        get;
        set => SetAndRaise(DefaultTransitionProperty, ref field, value);
    } = TransitioningContentControl.PageTransitionProperty.GetDefaultValue(typeof(TransitioningContentControl))
     ?? new CrossFade(TimeSpan.FromMilliseconds(197));

    public ICommand GoBackCommand => _goBackCommand;

    public bool CanGoBack => _history.Count > 0 || CanGoBackOutOfStack;

    public PageActivatorDelegate PageActivator { get; set; } = (t, _) => Activator.CreateInstance(t);

    public void ClearHistory() => _history.Clear();

    public void Navigate(Type page, object? parameter, IPageTransition? transition)
    {
        var content = PageActivator(page, parameter)
                   ?? throw new InvalidOperationException($"Activating {page.Name} gets null page model");
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
        else
        {
            throw new InvalidOperationException("No previous page in the stack");
        }

        RaisePropertyChanged(CanGoBackProperty, true, CanGoBack);
        _goBackCommand.OnCanExecutedChanged();
    }

    private void UpdateContent(object? content, IPageTransition transition, bool reverse)
    {
        _current = new ValueTuple<object?, IPageTransition, bool>(content, transition, reverse);
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
            _doubleArrangeSafeLock = false;
            _currentToken?.Cancel();
            var cancel = new CancellationTokenSource();
            _currentToken = cancel;
            var (from, to) = _presenter.Content is not null ? (_presenter, _presenter2) : (_presenter2, _presenter);

            (from.ZIndex, to.ZIndex) = (0, 1);
            (from.IsVisible, to.IsVisible) = (true, true);
            to.Content = _current.Value.Content;
            _current
               .Value.Transition.Start(from, to, !_current.Value.Reverse, cancel.Token)
               .ContinueWith(_ =>
                             {
                                 if (cancel.IsCancellationRequested)
                                     return;
                                 (from.IsVisible, to.IsVisible) = (false, true);
                                 from.Content = null;
                                 // NOTE: ContentControl.Content 改变会移除 from.Content 自 LogicalChildren，这会导致 from.Content 的 StaticResource 全部失效
                                 //  因此要放在动画结束 from 退出时对 Content 进行设置
                                 Content = to.Content;
                             },
                             TaskScheduler.FromCurrentSynchronizationContext());
        }

        return rv;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _presenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
        _presenter2 = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter2);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ContentProperty)
        {
            if (change.OldValue is ILogical oldChild)
                LogicalChildren.Remove(oldChild);

            if (change.NewValue is ILogical newChild)
                LogicalChildren.Add(newChild);
        }
    }

    #region Nested type: FrameFrame

    // protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    // {
    //     base.OnApplyTemplate(e);
    //     _presenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
    //     _presenter2 = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter2);
    // }

    public record FrameFrame(Type Page, object? Parameter, IPageTransition? Transition);

    #endregion

    #region Nested type: InternalGoBackCommand

    private class InternalGoBackCommand(Frame host) : ICommand
    {
        internal void OnCanExecutedChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        #region ICommand Members

        public bool CanExecute(object? parameter) => host.CanGoBack;

        public void Execute(object? parameter) => host.GoBack();

        public event EventHandler? CanExecuteChanged;

        #endregion
    }

    #endregion
}