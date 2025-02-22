using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.Threading;
using Huskui.Avalonia.Transitions;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":present")]
[TemplatePart(PART_Stage, typeof(Border))]
[TemplatePart(PART_ItemsPresenter, typeof(ItemsPresenter))]
public class OverlayHost : TemplatedControl
{
    public const string PART_ItemsPresenter = nameof(PART_ItemsPresenter);
    public const string PART_Stage = nameof(PART_Stage);

    public static readonly DirectProperty<OverlayHost, OverlayItems> ItemsProperty = AvaloniaProperty.RegisterDirect<OverlayHost, OverlayItems>(nameof(Items), o => o.Items, (o, v) => o.Items = v);

    public static readonly DirectProperty<OverlayHost, bool> IsPresentProperty = AvaloniaProperty.RegisterDirect<OverlayHost, bool>(nameof(IsPresent), o => o.IsPresent, (o, v) => o.IsPresent = v);


    public static readonly DirectProperty<OverlayHost, IPageTransition> TransitionProperty = AvaloniaProperty.RegisterDirect<OverlayHost, IPageTransition>(nameof(Transition), o => o.Transition, (o, v) => o.Transition = v);

    private bool _isPresent;

    private OverlayItems _items = new();

    private Border? _stage;

    private IPageTransition _transition = new PageCoverOverTransition(null, DirectionFrom.Bottom);

    [Content]
    public OverlayItems Items
    {
        get => _items;
        set => SetAndRaise(ItemsProperty, ref _items, value);
    }

    public bool IsPresent
    {
        get => _isPresent;
        set => SetAndRaise(IsPresentProperty, ref _isPresent, value);
    }

    public IPageTransition Transition
    {
        get => _transition;
        set => SetAndRaise(TransitionProperty, ref _transition, value);
    }

    protected override Type StyleKeyOverride => typeof(OverlayHost);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _stage = e.NameScope.Find<Border>(PART_Stage);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsPresentProperty)
            PseudoClasses.Set(":present", change.GetNewValue<bool>());
    }

    public void Pop(object control)
    {
        IsVisible = true;
        OverlayItem? item = new() { Content = control };
        Items.Add(item);
        item.DismissCommand = new InternalDismissCommand(this, item);


        // Make control attached to visual tree ensuring its parent is valid
        // Make OnApplyTemplate called and _stage bound
        UpdateLayout();
        // if (control is Visual visual) Transition.Start(null, visual, true, CancellationToken.None);
        var transition = item.Transition ?? Transition;
        transition.Start(null, item.ContentPresenter, true, CancellationToken.None);

        if (Items.Count == 1)
        {
            IsPresent = true;
            ArgumentNullException.ThrowIfNull(_stage);
            StageInAnimation.RunAsync(_stage);
        }
    }

    public void Dismiss(OverlayItem item)
    {
        var transition = item.Transition ?? Transition;
        transition.Start(item.ContentPresenter, null, false, CancellationToken.None).ContinueWith(_ => Dispatcher.UIThread.Post(Clean));
        return;

        void Clean()
        {
            Items.Remove(item);
            if (Items.Count == 0)
            {
                IsPresent = false;
                ArgumentNullException.ThrowIfNull(_stage);
                StageOutAnimation.RunAsync(_stage).ContinueWith(_ => Dispatcher.UIThread.Post(() => IsVisible = false));
            }
        }
    }

    public void Dismiss() => Dismiss(Items.Last());

    #region Nested type: InternalDismissCommand

    private class InternalDismissCommand(OverlayHost host, OverlayItem item) : ICommand
    {
        #region ICommand Members

        public bool CanExecute(object? parameter) => host.Items.Contains(item);

        public void Execute(object? parameter) => host.Dismiss(item);

        public event EventHandler? CanExecuteChanged;

        #endregion

        internal void OnCanExecutedChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region StageInAnimation & StageOutAnimation

    private static readonly Animation StageInAnimation = new() { FillMode = FillMode.Forward, Duration = TimeSpan.FromMilliseconds(146), Easing = new SineEaseOut(), Children = { new KeyFrame { Cue = new Cue(0d), Setters = { new Setter { Property = OpacityProperty, Value = 0d } } }, new KeyFrame { Cue = new Cue(1d), Setters = { new Setter { Property = OpacityProperty, Value = 1d } } } } };

    private static readonly Animation StageOutAnimation = new() { FillMode = FillMode.Forward, Duration = TimeSpan.FromMilliseconds(146), Easing = new SineEaseOut(), Children = { new KeyFrame { Cue = new Cue(0d), Setters = { new Setter { Property = OpacityProperty, Value = 1d } } }, new KeyFrame { Cue = new Cue(1d), Setters = { new Setter { Property = OpacityProperty, Value = 0d } } } } };

    #endregion

    #region ContentAlignment

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty = ContentControl.HorizontalContentAlignmentProperty.AddOwner<OverlayHost>();

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty = ContentControl.VerticalContentAlignmentProperty.AddOwner<OverlayHost>();

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    #endregion
}