using System;
using Avalonia.Data.Converters;
using Humanizer;
using Huskui.Avalonia.Converters;

namespace Polymerium.App.Converters;

public static class HumanizerConverters
{
    public static IValueConverter ByteSize { get; } = new RelayConverter((v) => v switch
    {
        long l => Humanizer.ByteSize.FromBytes(l).Humanize(),
        int i => Humanizer.ByteSize.FromBytes(i).Humanize(),
        _ => v
    });

    public static IValueConverter DateTime { get; } = new RelayConverter(v => v switch
    {
        DateTimeOffset offset => offset.Humanize(),
        DateTime time => time.Humanize(),
        _ => v
    });
}
