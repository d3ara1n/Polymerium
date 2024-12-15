using System.Windows.Input;
using Avalonia;
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

    private ContentPresenter? _contentPresenter;

    public ContentPresenter ContentPresenter => _contentPresenter ??
                                                throw new InvalidOperationException(
                                                    $"{nameof(ContentPresenter)} is not found from the template");

    public static readonly DirectProperty<OverlayItem, int> DistanceProperty =
        AvaloniaProperty.RegisterDirect<OverlayItem, int>(nameof(Distance), o => o.Distance, (o, v) => o.Distance = v);

    private int _distance;

    public int Distance
    {
        get => _distance;
        set
        {
            if (SetAndRaise(DistanceProperty, ref _distance, value)) ZIndex = -value;

            PseudoClasses.Set(":active", value == 0);
        }
    }

    public static readonly DirectProperty<OverlayItem, ICommand?> DismissCommandProperty =
        AvaloniaProperty.RegisterDirect<OverlayItem, ICommand?>(nameof(DismissCommand), o => o.DismissCommand,
            (o, v) => o.DismissCommand = v);
    
    private ICommand? _dismissCommand;
    
    public ICommand? DismissCommand
    {
        get => _dismissCommand;
        set => SetAndRaise(DismissCommandProperty, ref _dismissCommand, value);
    }

    protected override Type StyleKeyOverride => typeof(OverlayItem);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _contentPresenter = e.NameScope.Find<ContentPresenter>(PART_ContentPresenter);
    }
}