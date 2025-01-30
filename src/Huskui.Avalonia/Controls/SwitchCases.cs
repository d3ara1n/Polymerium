using Avalonia.Collections;

namespace Huskui.Avalonia.Controls;

// https://github.com/CommunityToolkit/Windows/blob/main/components/Primitives/src/SwitchPresenter/SwitchHelpers.cs
public class SwitchCases : AvaloniaList<SwitchCase>
{
    internal SwitchCase? EvaluateCases(object? value, Type targetType)
    {
        if (Count == 0)
            // If we have no cases, then we can't match anything.
            return null;

        return this.FirstOrDefault(@case => CompareValues(value, @case.Value, targetType)) ?? this.FirstOrDefault();
    }

    private static bool CompareValues(object? compare, object? value, Type? targetType)
    {
        if (compare == null || value == null) return compare == value;

        if (targetType == null ||
            (targetType == compare.GetType() &&
             targetType == value.GetType()))
            // Default direct object comparison or we're all the proper type
            return compare.Equals(value);

        if (compare.GetType() == targetType)
        {
            // If we have a TargetType and the first value is the right type
            // Then our 2nd value isn't, so convert to string and coerce.
            var valueBase2 = ConvertValue(targetType, value);

            return compare.Equals(valueBase2);
        }

        // Neither of our two values matches the type so
        // we'll convert both to a String and try and coerce it to the proper type.
        var compareBase = ConvertValue(targetType, compare);

        var valueBase = ConvertValue(targetType, value);

        return compareBase.Equals(valueBase);
    }

    internal static object ConvertValue(Type targetType, object value)
    {
        if (targetType.IsInstanceOfType(value)) return value;

        if (targetType.IsEnum && value is string str)
        {
            if (Enum.TryParse(targetType, str, out var result)) return result;

            throw new InvalidOperationException("The requested enum value was not present in the provided type.");
        }

        return Convert.ChangeType(value, targetType);
    }
}