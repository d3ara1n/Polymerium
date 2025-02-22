using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Huskui.Avalonia.Controls;

public class SwitchPresenter : ContentControl
{
    public static readonly DirectProperty<SwitchPresenter, SwitchCases> CasesProperty = AvaloniaProperty.RegisterDirect<SwitchPresenter, SwitchCases>(nameof(Cases), o => o.Cases, (o, v) => o.Cases = v);

    public static readonly DirectProperty<SwitchPresenter, SwitchCase?> CurrentCaseProperty = AvaloniaProperty.RegisterDirect<SwitchPresenter, SwitchCase?>(nameof(CurrentCase), o => o.CurrentCase, (o, v) => o.CurrentCase = v);

    public static readonly DirectProperty<SwitchPresenter, object?> ValueProperty = AvaloniaProperty.RegisterDirect<SwitchPresenter, object?>(nameof(Value), o => o.Value, (o, v) => o.Value = v);

    public static readonly DirectProperty<SwitchPresenter, Type> TargetTypeProperty = AvaloniaProperty.RegisterDirect<SwitchPresenter, Type>(nameof(TargetType), o => o.TargetType, (o, v) => o.TargetType = v);

    private SwitchCases _cases = [];

    private SwitchCase? _currentCase;

    private Type _targetType = typeof(object);

    private object? _value;

    [Content]
    public SwitchCases Cases
    {
        get => _cases;
        set => SetAndRaise(CasesProperty, ref _cases, value);
    }

    public SwitchCase? CurrentCase
    {
        get => _currentCase;
        set => SetAndRaise(CurrentCaseProperty, ref _currentCase, value);
    }

    public object? Value
    {
        get => _value;
        set
        {
            if (SetAndRaise(ValueProperty, ref _value, value))
                EvaluateCases();
        }
    }

    public Type TargetType
    {
        get => _targetType;
        set => SetAndRaise(TargetTypeProperty, ref _targetType, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        EvaluateCases();
    }

    private void EvaluateCases()
    {
        if (CurrentCase?.Value != null && CurrentCase.Value.Equals(Value))
            // If the current case we're on already matches our current value,
            // then we don't have any work to do.
            return;

        var result = Cases.EvaluateCases(Value, TargetType);

        // Only bother changing things around if we actually have a new case. (this should handle prior null case as well)
        if (result != CurrentCase)
        {
            // If we don't have any cases or default, setting these to null is what we want to be blank again.
            Content = result?.Content;
            CurrentCase = result;
        }
    }
}