using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls;

[PseudoClasses(":active")]
[TemplatePart(PART_ContentPresenter, typeof(ContentPresenter))]
public class OverlayItem : ContentControl
{
    public const string PART_ContentPresenter = nameof(PART_ContentPresenter);

    public static readonly DirectProperty<OverlayItem, int> DistanceProperty =
        AvaloniaProperty.RegisterDirect<OverlayItem, int>(nameof(Distance), o => o.Distance, (o, v) => o.Distance = v);

    public static readonly DirectProperty<OverlayItem, ICommand?> DismissCommandProperty =
        AvaloniaProperty.RegisterDirect<OverlayItem, ICommand?>(nameof(DismissCommand),
                                                                o => o.DismissCommand,
                                                                (o, v) => o.DismissCommand = v);

    public static readonly DirectProperty<OverlayItem, IPageTransition?> TransitionProperty =
        AvaloniaProperty.RegisterDirect<OverlayItem, IPageTransition?>(nameof(Transition),
                                                                       o => o.Transition,
                                                                       (o, v) => o.Transition = v);

    private ContentPresenter? _contentPresenter;
    
    public IPageTransition? Transition
    {
        get;
        set => SetAndRaise(TransitionProperty, ref field, value);
    }

    public ContentPresenter ContentPresenter =>
        _contentPresenter
     ?? throw new InvalidOperationException($"{nameof(ContentPresenter)} is not found from the template");

    public int Distance
    {
        get;
        set
        {
            if (SetAndRaise(DistanceProperty, ref field, value))
                ZIndex = -value;

            PseudoClasses.Set(":active", value == 0);
        }
    }

    public ICommand? DismissCommand
    {
        get;
        set => SetAndRaise(DismissCommandProperty, ref field, value);
    }

    protected override Type StyleKeyOverride => typeof(OverlayItem);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentPresenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
    }
}