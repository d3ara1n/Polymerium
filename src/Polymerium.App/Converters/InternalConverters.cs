using System;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Humanizer;
using Huskui.Avalonia.Converters;
using Polymerium.App.Models;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;
using Trident.Core.Igniters;
using Resources = Polymerium.App.Properties.Resources;

namespace Polymerium.App.Converters;

public static class InternalConverters
{
    public static IMultiValueConverter OneOr { get; } =
        new RelayMultiConverter(
            (v, _, _) =>
            {
                if (v is [bool b, double d])
                {
                    return b ? 1.0d : d;
                }

                return 1.0d;
            }
        );

    public static IMultiValueConverter ZeroOr { get; } =
        new RelayMultiConverter(
            (v, _, _) =>
            {
                if (v is [bool b, double d])
                {
                    return b ? 0.0d : d;
                }

                return 0.0d;
            }
        );

    public static IValueConverter EnumTypeToList { get; } =
        new RelayConverter(
            (v, _) =>
            {
                if (v is Type { IsEnum: true } t)
                {
                    return Enum.GetValues(t);
                }

                return v;
            }
        );

    public static IValueConverter UnsignedLongToMiBDoubleConverter { get; } =
        new RelayConverter(v =>
            v switch
            {
                ulong l => (double)l / 1024 / 1024,
                _ => v,
            }
        );

    public static IValueConverter UnsignedLongToGiBDoubleConverter { get; } =
        new RelayConverter(v =>
            v switch
            {
                ulong l => (double)l / 1024 / 1024 / 1024,
                _ => v,
            }
        );

    public static IValueConverter ByteSizeConverter { get; } =
        new RelayConverter(
            (v, _) =>
                v switch
                {
                    int i => ByteSize.FromBytes(i).Humanize(),
                    long l => ByteSize.FromBytes(l).Humanize(),
                    float f => ByteSize.FromBytes(f).Humanize(),
                    double d => ByteSize.FromBytes(d).Humanize(),
                    _ => v,
                }
        );

    public static IMultiValueConverter LatencyToColorBrush { get; } =
        new RelayMultiConverter(
            (values, _, _) =>
            {
                if (values is [double latency, ConnectionTestStatus status])
                {
                    return status switch
                    {
                        // 不可用: 红色 (Danger)
                        ConnectionTestStatus.Failed => Brushes.Red,
                        ConnectionTestStatus.Success => latency switch
                        {
                            // 根据延迟返回不同颜色
                            // < 1000ms: 绿色 (Success)
                            // 100-300ms: 黄色 (Warning)
                            < 1000 => Application.Current?.TryGetResource(
                                "ControlSuccessBackgroundBrush",
                                null,
                                out var resource
                            ) == true
                                ? resource as SolidColorBrush
                                : Brushes.Green,
                            _ => Application.Current?.TryGetResource(
                                "ControlWarningBackgroundBrush",
                                null,
                                out var resource
                            ) == true
                                ? resource as SolidColorBrush
                                : Brushes.Orange,
                        },
                        _ => Brushes.Gray,
                    };
                }

                return Brushes.Gray;
            }
        );
}
