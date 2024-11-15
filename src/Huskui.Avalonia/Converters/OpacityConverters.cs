using Avalonia.Data.Converters;

namespace Huskui.Avalonia.Converters;

public static class OpacityConverters
{
    public static IValueConverter IsNotNull { get; } = new RelayConverter((v, _) => v is not null ? 1.0D : 0.0D);
    public static IValueConverter IsNull { get; } = new RelayConverter((v, _) => v is null ? 1.0D : 0.0D);
}