using System;
using System.Diagnostics.CodeAnalysis;

namespace Polymerium.Avalonia.Utilities;

public static class SkinHelper
{
    private const string MOJANG_SCHEME = "mojang";
    private const string ASSET_SCHEME = "asset";

    public enum SourceKind { Mojang, Asset, Remote }

    public static string MojangSource(string uuid)
    {
        if (!Guid.TryParse(uuid, out var parsed))
        {
            throw new ArgumentException("A Mojang skin source requires a UUID.", nameof(uuid));
        }

        return MOJANG_SCHEME + ":" + parsed.ToString("N");
    }

    public static string AssetSource(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return ASSET_SCHEME + ":" + key;
    }

    public static bool TryParseSource(string? source, out SourceKind kind, [NotNullWhen(true)] out string? value)
    {
        kind = default;
        value = null;
        if (InternalUriHelper.HasScheme(source, MOJANG_SCHEME))
        {
            if (!Guid.TryParse(source![(MOJANG_SCHEME.Length + 1)..], out var uuid))
            {
                return false;
            }

            kind = SourceKind.Mojang;
            value = uuid.ToString("N");
            return true;
        }

        if (InternalUriHelper.HasScheme(source, ASSET_SCHEME))
        {
            var key = source![(ASSET_SCHEME.Length + 1)..];
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            kind = SourceKind.Asset;
            value = key;
            return true;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        kind = SourceKind.Remote;
        value = uri.AbsoluteUri;
        return true;
    }
}
