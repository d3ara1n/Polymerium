using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using Avalonia.Threading;
using Huskui.Avalonia.Transitions;

namespace Huskui.Avalonia.Controls;

[TemplatePart(PART_ItemsPresenter, typeof(ItemsPresenter))]
public class OverlayHost : TemplatedControl
{
    public const string PART_ItemsPresenter = nameof(PART_ItemsPresenter);

    public static readonly DirectProperty<OverlayHost, OverlayItems> ItemsProperty =
        AvaloniaProperty.RegisterDirect<OverlayHost, OverlayItems>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

    private OverlayItems _items = new();

    [Content]
    public OverlayItems Items
    {
        get => _items;
        set => SetAndRaise(ItemsProperty, ref _items, value);
    }

    public static readonly DirectProperty<OverlayHost, IPageTransition> TransitionProperty =
        AvaloniaProperty.RegisterDirect<OverlayHost, IPageTransition>(nameof(Transition), o => o.Transition,
            (o, v) => o.Transition = v);

    private IPageTransition _transition = new PageCoverIn(direction: DirectionFrom.Bottom);

    public IPageTransition Transition
    {
        get => _transition;
        set => SetAndRaise(TransitionProperty, ref _transition, value);
    }

    public void Pop(object control)
    {
        IsVisible = true;
        var item = new OverlayItem
        {
            Content = control
        };
        Items.Add(item);

        item.DismissCommand = new InternalDismissCommand(this, item);

        // Make control attached to visual tree ensuring its parent is valid
        UpdateLayout();
        if (control is Visual visual) Transition.Start(null, visual, true, CancellationToken.None);
    }

    public void Dismiss(OverlayItem item)
    {
        if (item.Content is Visual visual)
            Transition.Start(visual, null, false, CancellationToken.None)
                .ContinueWith(_ => Dispatcher.UIThread.Post(() =>
                {
                    Items.Remove(item);
                    if (Items.Count == 0) IsVisible = false;
                }));
        else
            Items.Remove(item);
    }

    public void Dismiss()
    {
        Dismiss(Items.Last());
    }

    private class InternalDismissCommand(OverlayHost host, OverlayItem item) : ICommand
    {
        public bool CanExecute(object? parameter)
        {
            return host.Items.Contains(item);
        }

        public void Execute(object? parameter)
        {
            host.Dismiss(item);
        }

        public event EventHandler? CanExecuteChanged;

        internal void OnCanExecutedChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}