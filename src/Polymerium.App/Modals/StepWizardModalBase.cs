using Avalonia;
using Huskui.Avalonia.Controls;

namespace Polymerium.App.Modals;

public abstract class StepWizardModalBase : Modal
{
    public static readonly DirectProperty<StepWizardModalBase, object?> CurrentStepProperty =
        AvaloniaProperty.RegisterDirect<StepWizardModalBase, object?>(
            nameof(CurrentStep),
            o => o.CurrentStep,
            (o, v) => o.CurrentStep = v
        );

    public static readonly DirectProperty<StepWizardModalBase, bool> IsReversedProperty =
        AvaloniaProperty.RegisterDirect<StepWizardModalBase, bool>(
            nameof(IsReversed),
            o => o.IsReversed,
            (o, v) => o.IsReversed = v
        );

    public static readonly DirectProperty<StepWizardModalBase, bool> IsBackAvailableProperty =
        AvaloniaProperty.RegisterDirect<StepWizardModalBase, bool>(
            nameof(IsBackAvailable),
            o => o.IsBackAvailable,
            (o, v) => o.IsBackAvailable = v
        );

    public static readonly DirectProperty<StepWizardModalBase, bool> IsLastProperty =
        AvaloniaProperty.RegisterDirect<StepWizardModalBase, bool>(
            nameof(IsLast),
            o => o.IsLast,
            (o, v) => o.IsLast = v
        );

    public object? CurrentStep
    {
        get;
        set => SetAndRaise(CurrentStepProperty, ref field, value);
    }

    public bool IsReversed
    {
        get;
        set => SetAndRaise(IsReversedProperty, ref field, value);
    }

    public bool IsBackAvailable
    {
        get;
        set => SetAndRaise(IsBackAvailableProperty, ref field, value);
    }

    public bool IsLast
    {
        get;
        set => SetAndRaise(IsLastProperty, ref field, value);
    }
}
