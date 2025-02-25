using Avalonia.Collections;
using Huskui.Avalonia.Converters;

namespace Huskui.Avalonia.Controls;

// https://github.com/CommunityToolkit/Windows/blob/main/components/Primitives/src/SwitchPresenter/SwitchHelpers.cs
public class SwitchCases : AvaloniaList<SwitchCase>
{
    internal SwitchCase? EvaluateCases(object? value, Type targetType)
    {
        if (Count == 0)
            // If we have no cases, then we can't match anything.
            return null;

        return this.FirstOrDefault(@case => RelayConverter.CompareValues(value, @case.Value, targetType))
            ?? this.FirstOrDefault();
    }
}