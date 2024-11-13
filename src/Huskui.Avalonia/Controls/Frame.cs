using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_Container, typeof(TransitioningContentControl))]
public class Frame : ContentControl
{
    public const string PART_Container = nameof(PART_Container);
    private readonly Stack<FrameFrame> _history = new();
    public IEnumerable<FrameFrame> History => _history;

    private TransitioningContentControl? _container;

    public bool CanGoBack => _history.Count > 0;

    public PageActivatorDelegate PageActivator { get; set; } =
        (t, _) => Activator.CreateInstance(t);

    public void Navigate(Type page, object? parameter = null, IPageTransition? transition = null) =>
        Navigate(page, parameter, transition, false);

    private void Navigate(Type page, object? parameter, IPageTransition? transition, bool reverse)
    {
        var content = PageActivator(page, parameter) ?? throw new ArgumentNullException();
        ArgumentNullException.ThrowIfNull(_container);

        _history.Push(new FrameFrame(page, parameter, transition));
        _container.PageTransition = transition;
        _container.IsTransitionReversed = reverse;
        Content = content;
    }

    public void GoBack()
    {
        if (_history.TryPop(out var frame))
        {
            Navigate(frame.Page, frame.Parameter, frame.Transition, true);
        }
        else throw new InvalidOperationException("No previous page in the stack");
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _container = e.NameScope.Find<TransitioningContentControl>("PART_Container");
    }

    public record FrameFrame(Type Page, object? Parameter, IPageTransition? Transition);

    public delegate object? PageActivatorDelegate(Type page, object? parameter);
}