using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_Container, typeof(TransitioningContentControl))]
public class Frame : ContentControl
{
    public delegate object? PageActivatorDelegate(Type page, object? parameter);

    public const string PART_Container = nameof(PART_Container);

    public static readonly StyledProperty<IPageTransition?> DefaultTransitionProperty =
        AvaloniaProperty.Register<Frame, IPageTransition?>(nameof(DefaultTransition),
            TransitioningContentControl.PageTransitionProperty.GetDefaultValue(
                typeof(TransitioningContentControl)));

    public static readonly StyledProperty<bool> CanGoBackProperty =
        AvaloniaProperty.Register<Frame, bool>(nameof(CanGoBack));

    private readonly Stack<FrameFrame> _history = new();

    private TransitioningContentControl? _container;

    private FrameFrame? _current;
    public IEnumerable<FrameFrame> History => _history;

    private readonly InternalGoBackCommand _goBackCommand;
    public ICommand GoBackCommand => _goBackCommand;

    public Frame()
    {
        _goBackCommand = new InternalGoBackCommand(this);
    }

    public bool CanGoBack => GetValue(CanGoBackProperty);

    public PageActivatorDelegate PageActivator { get; set; } =
        (t, _) => Activator.CreateInstance(t);

    public IPageTransition? DefaultTransition
    {
        get => GetValue(DefaultTransitionProperty);
        set => SetValue(DefaultTransitionProperty, value);
    }

    public void Navigate(Type page, object? parameter = null, IPageTransition? transition = null)
    {
        Navigate(page, parameter, transition, false, true);
    }

    private void Navigate(Type page, object? parameter, IPageTransition? transition, bool reverse, bool stack)
    {
        var content = PageActivator(page, parameter) ?? throw new ArgumentNullException();
        ArgumentNullException.ThrowIfNull(_container);


        if (stack && _current is not null)
            _history.Push(_current);
        _current = new FrameFrame(page, parameter, transition);
        
        _container.PageTransition = transition ?? DefaultTransition;
        _container.IsTransitionReversed = reverse;
        Content = content;
        SetValue(CanGoBackProperty, _history.Count > 0);
        _goBackCommand.OnCanExecutedChanged();
    }

    public void GoBack()
    {
        if (_history.TryPop(out var frame))
            Navigate(frame.Page, frame.Parameter, frame.Transition, true, false);
        else throw new InvalidOperationException("No previous page in the stack");
        SetValue(CanGoBackProperty, _history.Count > 0);
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
        public bool CanExecute(object? parameter) => host.CanGoBack;

        public void Execute(object? parameter) => host.GoBack();

        public event EventHandler? CanExecuteChanged;

        internal void OnCanExecutedChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}