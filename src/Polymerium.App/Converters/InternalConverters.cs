using System;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Humanizer;
using Huskui.Avalonia;
using Huskui.Avalonia.Converters;
using Huskui.Avalonia.Models;
using Polymerium.App.Models;

namespace Polymerium.App.Converters;

public static class InternalConverters
{

    public static IMultiValueConverter OneOr { get; } = new RelayMultiConverter((v, _, _) =>
    {
        if (v is [bool b, double d])
        {
            return b ? 1.0d : d;
        }

        return 1.0d;
    });

    public static IMultiValueConverter ZeroOr { get; } = new RelayMultiConverter((v, _, _) =>
    {
        if (v is [bool b, double d])
        {
            return b ? 0.0d : d;
        }

        return 0.0d;
    });

    public static IValueConverter EnumTypeToList { get; } = new RelayConverter((v, _) =>
    {
        if (v is Type { IsEnum: true } t)
        {
            return Enum.GetValues(t);
        }

        return v;
    });

    public static IValueConverter UnsignedLongToMiBDoubleConverter { get; } = new RelayConverter(v => v switch
    {
        ulong l => (double)l / 1024 / 1024,
        _ => v,
    });

    public static IValueConverter UnsignedLongToGiBDoubleConverter { get; } = new RelayConverter(v => v switch
    {
        ulong l => (double)l / 1024 / 1024 / 1024,
        _ => v,
    });

    public static IValueConverter AccentColorToBrush { get; } = new RelayConverter((v, _) => v is AccentColor accent
        ? new SolidColorBrush(accent switch
        {
            AccentColor.System => Colors.Transparent,
            AccentColor.Neutral => Color.FromRgb(0x8D, 0x8D, 0x8D),
            AccentColor.Tomato => Color.FromRgb(0xDD, 0x44, 0x25),
            AccentColor.Red => Color.FromRgb(0xDC, 0x3E, 0x42),
            AccentColor.Ruby => Color.FromRgb(0xDC, 0x3B, 0x5D),
            AccentColor.Crimson => Color.FromRgb(0xDF, 0x34, 0x78),
            AccentColor.Pink => Color.FromRgb(0xCF, 0x38, 0x97),
            AccentColor.Plum => Color.FromRgb(0xAB, 0x4A, 0xBA),
            AccentColor.Purple => Color.FromRgb(0x83, 0x47, 0xB9),
            AccentColor.Violet => Color.FromRgb(0x6E, 0x56, 0xCF),
            AccentColor.Iris => Color.FromRgb(0x51, 0x51, 0xCD),
            AccentColor.Indigo => Color.FromRgb(0x33, 0x58, 0xD4),
            AccentColor.Blue => Color.FromRgb(0x00, 0x90, 0xFF),
            AccentColor.Cyan => Color.FromRgb(0x07, 0x97, 0xB9),
            AccentColor.Teal => Color.FromRgb(0x0D, 0x9B, 0x8A),
            AccentColor.Jade => Color.FromRgb(0x29, 0xA3, 0x83),
            AccentColor.Green => Color.FromRgb(0x2B, 0x9A, 0x66),
            AccentColor.Grass => Color.FromRgb(0x3E, 0x9B, 0x4F),
            AccentColor.Bronze => Color.FromRgb(0x95, 0x74, 0x68),
            AccentColor.Gold => Color.FromRgb(0x8C, 0x7A, 0x5E),
            AccentColor.Orange => Color.FromRgb(0xEF, 0x5F, 0x00),
            AccentColor.Amber => Color.FromRgb(0xFF, 0xC5, 0x3D),
            AccentColor.Yellow => Color.FromRgb(0xFF, 0xDC, 0x00),
            AccentColor.Lime => Color.FromRgb(0xBD, 0xEE, 0x63),
            AccentColor.Mint => Color.FromRgb(0x7D, 0xE0, 0xCB),
            AccentColor.Sky => Color.FromRgb(0x7C, 0xE2, 0xFE),
            _ => Colors.Transparent,
        })
        : AvaloniaProperty.UnsetValue);

    public static IMultiValueConverter LatencyToColorBrush { get; } = new RelayMultiConverter((values, _, _) =>
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
                    < 1000 => Application.Current?.TryGetResource("ControlSuccessBackgroundBrush",
                                                                  null,
                                                                  out var resource)
                           == true
                                  ? resource as SolidColorBrush
                                  : Brushes.Green,
                    _ => Application.Current?.TryGetResource("ControlWarningBackgroundBrush", null, out var resource)
                      == true
                             ? resource as SolidColorBrush
                             : Brushes.Orange,
                },
                _ => Brushes.Gray,
            };
        }

        return Brushes.Gray;
    });

    private static IBrush? DiffAddedBrush;

    private static IBrush? DiffRemovedBrush;

    private static IBrush? DiffEmptyBrush;

    private static IBrush? DiffModifiedBrush;
    private static ThemeVariant? DiffTheme;

    private static void EnsureDiffBrushes()
    {
        if (DiffTheme == Application.Current?.ActualThemeVariant)
        {
            return;
        }

        DiffTheme = Application.Current?.ActualThemeVariant;
        DiffAddedBrush =
            Application.Current?.TryGetResource("ControlSuccessTranslucentHalfBackgroundBrush", null, out var res1) == true
                ? res1 as IBrush
                : new SolidColorBrush(Color.FromArgb(40, 0x40, 0xA0, 0x40));
        DiffRemovedBrush =
            Application.Current?.TryGetResource("ControlDangerTranslucentHalfBackgroundBrush", null, out var res2) == true
                ? res2 as IBrush
                : new SolidColorBrush(Color.FromArgb(40, 0xA0, 0x40, 0x40));
        DiffEmptyBrush =
            Application.Current?.TryGetResource("ControlTranslucentHalfBackgroundBrush", null, out var res3) == true
                ? res3 as IBrush
                : new SolidColorBrush(Color.FromArgb(20, 0x80, 0x80, 0x80));
        DiffModifiedBrush =
            Application.Current?.TryGetResource("ControlAccentTranslucentHalfBackgroundBrush", null, out var res4) == true
                ? res4 as IBrush
                : new SolidColorBrush(Color.FromArgb(40, 0x40, 0x80, 0xC0));
    }

    public static IValueConverter DiffLineKindToBackground { get; } = new RelayConverter((v, _) =>
    {
        EnsureDiffBrushes();
        return v is DiffLineKind kind
                   ? kind switch
                   {
                       DiffLineKind.Added => DiffAddedBrush,
                       DiffLineKind.Removed => DiffRemovedBrush,
                       DiffLineKind.Empty => DiffEmptyBrush,
                       DiffLineKind.Modified => DiffModifiedBrush,
                       _ => Brushes.Transparent,
                   }
                   : AvaloniaProperty.UnsetValue;
    });

    public static IMultiValueConverter MemoryUsageToColorBrush { get; } = new RelayMultiConverter(v =>
    {
        if (v is [uint current, uint max])
        {
            if (max is > 0)
            {
                var percent = (float)current / max;
                if (percent > 1.0)
                {
                    return Application.Current?.TryGetResource("ControlDangerBackgroundBrush", null, out var resource)
                        == true
                               ? resource as SolidColorBrush
                               : Brushes.Red;
                }
                else if (percent > 0.8)
                {
                    return Application.Current?.TryGetResource("ControlWarningBackgroundBrush", null, out var resource)
                        == true
                               ? resource as SolidColorBrush
                               : Brushes.Orange;
                }
            }
        }

        return AvaloniaProperty.UnsetValue;
    });
}
