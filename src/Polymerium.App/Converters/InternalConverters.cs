using System;
using Avalonia.Data.Converters;
using Huskui.Avalonia.Converters;
using Polymerium.App.Properties;
using Polymerium.Trident.Igniters;
using Trident.Abstractions.Repositories.Resources;

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

    public static IValueConverter Weird { get; } = new RelayConverter((v, _) =>
    {
        if (v is double d)
            return Math.Min(Math.Max((1 - d) * 2, 0.0d), 1.0d);

        return v;
    });

    public static IValueConverter LocalizedResourceKindConverter { get; } = new RelayConverter((v, _) =>
    {
        if (v is ResourceKind kind)
            return kind switch
            {
                ResourceKind.Modpack => Resources.ResourceKind_Modpack,
                ResourceKind.Mod => Resources.ResourceKind_Mod,
                ResourceKind.ResourcePack => Resources.ResourceKind_ResourcePack,
                ResourceKind.ShaderPack => Resources.ResourceKind_ShaderPack,
                ResourceKind.DataPack => Resources.ResourceKind_DataPack,
                ResourceKind.World => Resources.ResourceKind_World,
                _ => kind.ToString()
            };
        return v;
    });

    public static IValueConverter LocalizedLaunchModeConverter { get; } = new RelayConverter((v, _) =>
    {
        if (v is LaunchMode mode)
            return mode switch
            {
                LaunchMode.Managed => Resources.LaunchMode_Managed,
                LaunchMode.FireAndForget => Resources.LaunchMode_FireAndForget,
                LaunchMode.Debug => Resources.LaunchMode_Debug,
                _ => mode.ToString()
            };
        return v;
    });
}