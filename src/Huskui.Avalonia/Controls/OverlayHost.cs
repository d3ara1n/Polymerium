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

[TemplatePart(PART_Stage, typeof(Border))]
[TemplatePart(PART_ItemsPresenter, typeof(ItemsPresenter))]
public class OverlayHost : TemplatedControl
{
    public const string PART_ItemsPresenter = nameof(PART_ItemsPresenter);
    public const string PART_Stage = nameof(PART_Stage);

    #region _stageInAnimation & _stageOutAnimation

    private static readonly Animation _stageInAnimation = new()
    {
        FillMode = FillMode.Forward,
        Duration = TimeSpan.FromMilliseconds(146),
        Easing = new SineEaseOut(),
        Children =
        {
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter
                    {
                        Property = OpacityProperty,
                        Value = 0d
                    }
                }
            },
            new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter
                    {
                        Property = OpacityProperty,
                        Value = 1d
                    }
                }
            }
        }
    };

    private static readonly Animation _stageOutAnimation = new()
    {
        FillMode = FillMode.Forward,
        Duration = TimeSpan.FromMilliseconds(146),
        Easing = new SineEaseOut(),
        Children =
        {
            new KeyFrame
            {
                Cue = new Cue(0d),
                Setters =
                {
                    new Setter
                    {
                        Property = OpacityProperty,
                        Value = 1d
                    }
                }
            },
            new KeyFrame
            {
                Cue = new Cue(1d),
                Setters =
                {
                    new Setter
                    {
                        Property = OpacityProperty,
                        Value = 0d
                    }
                }
            }
        }
    };

    #endregion

    #region ContentAlignment

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<OverlayHost>();

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<OverlayHost>();

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

    private IPageTransition _transition = new PageCoverOver(null, DirectionFrom.Bottom);

    public IPageTransition Transition
    {
        get => _transition;
        set => SetAndRaise(TransitionProperty, ref _transition, value);
    }

    protected override Type StyleKeyOverride => typeof(OverlayHost);

    private Border? _stage;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _stage = e.NameScope.Find<Border>(PART_Stage);
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
        // Make OnApplyTemplate called and _stage bound
        UpdateLayout();
        // if (control is Visual visual) Transition.Start(null, visual, true, CancellationToken.None);
        Transition.Start(null, item.ContentPresenter, true, CancellationToken.None);

        #region Smoke Background Animation

        if (Items.Count == 1)
        {
            ArgumentNullException.ThrowIfNull(_stage);
            _stageInAnimation.RunAsync(_stage);
        }

        #endregion
    }

    public void Dismiss(OverlayItem item)
    {
        void Clean()
        {
            Items.Remove(item);
            if (Items.Count == 0)
            {
                ArgumentNullException.ThrowIfNull(_stage);
                _stageOutAnimation.RunAsync(_stage)
                    .ContinueWith(_ => Dispatcher.UIThread.Post(() => IsVisible = false));
            }
        }

        Transition.Start(item.ContentPresenter, null, false, CancellationToken.None)
            .ContinueWith(_ => Dispatcher.UIThread.Post(Clean));
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