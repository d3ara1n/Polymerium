﻿using System.Windows.Input;
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


    private ICommand? _dismissCommand;

    private int _distance;

    private IPageTransition? _transition;

    public IPageTransition? Transition
    {
        get => _transition;
        set => SetAndRaise(TransitionProperty, ref _transition, value);
    }

    public ContentPresenter ContentPresenter =>
        _contentPresenter
     ?? throw new InvalidOperationException($"{nameof(ContentPresenter)} is not found from the template");

    public int Distance
    {
        get => _distance;
        set
        {
            if (SetAndRaise(DistanceProperty, ref _distance, value))
                ZIndex = -value;

            PseudoClasses.Set(":active", value == 0);
        }
    }

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