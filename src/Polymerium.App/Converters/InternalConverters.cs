using Avalonia.Data.Converters;
using Huskui.Avalonia.Converters;

namespace Polymerium.App.Converters;

public static class InternalConverters
{
    public static IMultiValueConverter OneOr { get; } = new RelayMultiConverter((v, _, _) =>
    {
        if (v is [bool b, double d])
            return b ? 1.0d : d;

        return 1.0d;
    });

    public static IMultiValueConverter ZeroOr { get; } = new RelayMultiConverter((v, _, _) =>
    {
        if (v is [bool b, double d])
            return b ? 0.0d : d;

        return 0.0d;
    });
}