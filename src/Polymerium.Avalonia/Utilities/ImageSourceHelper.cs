using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Web;
using Polymerium.Avalonia.Rendering;

namespace Polymerium.Avalonia.Utilities;

public static class ImageSourceHelper
{
    public const string ARCHIVE_SCHEME = "archive";
    public const string THUMBNAIL_SCHEME = "thumbnail";
    public const string SKIN_SCHEME = "skin";
    public const string DATA_SCHEME = "data";
    private const int MAX_THUMBNAIL_WIDTH = 4096;

    public static Uri FromFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return new UriBuilder(new Uri(fullPath))
        {
            Query = "version=" + File.GetLastWriteTimeUtc(fullPath).Ticks.ToString(CultureInfo.InvariantCulture)
        }.Uri;
    }

    public static Uri Archive(string path, string entry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entry);
        return Create(ARCHIVE_SCHEME, new() { ["source"] = FromFile(path).AbsoluteUri, ["entry"] = entry });
    }

    public static Uri RelocateArchive(Uri source, string path) =>
        TryParseArchive(source.AbsoluteUri, out _, out var entry) ? Archive(path, entry) : source;

    public static Uri Thumbnail(Uri source, int width)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsAbsoluteUri)
        {
            throw new ArgumentException("A thumbnail requires an absolute image source URI.", nameof(source));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MAX_THUMBNAIL_WIDTH);
        return Create(THUMBNAIL_SCHEME, new()
        {
            ["source"] = source.AbsoluteUri,
            ["width"] = width.ToString(CultureInfo.InvariantCulture)
        });
    }

    public static Uri Skin(SkinViewType view, string source)
    {
        if (!Enum.IsDefined(view))
        {
            throw new ArgumentOutOfRangeException(nameof(view));
        }

        if (!SkinHelper.TryParseSource(source, out _, out _))
        {
            throw new ArgumentException("A skin requires a supported source.", nameof(source));
        }

        return Create(SKIN_SCHEME, new()
        {
            ["type"] = view.ToString().ToLowerInvariant(),
            ["src"] = source
        });
    }

    public static Uri FromBase64(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        payload = payload.Trim();
        var source = InternalUriHelper.HasScheme(payload, DATA_SCHEME) ? payload : "data:image/png;base64," + payload;
        if (!TryParseData(source, out _))
        {
            throw new ArgumentException("Only Base64 image data URIs are supported.", nameof(payload));
        }

        return new Uri(source, UriKind.Absolute);
    }

    private static Uri Create(string scheme, NameValueCollection parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query.Add(parameters);
        return new UriBuilder(scheme, "local") { Query = query.ToString() }.Uri;
    }

    private static bool TryGetParameters(string? source, string scheme,
        [NotNullWhen(true)] out NameValueCollection? parameters, params string[] keys)
    {
        parameters = null;
        if (!InternalUriHelper.TryParse(source, scheme, out var uri)
            || !uri.Host.Equals("local", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath is not ("" or "/")
            || !InternalUriHelper.HasValidEscaping(source!))
        {
            return false;
        }

        var pathAndQuery = source!.AsSpan(scheme.Length + "://local".Length);
        var queryStart = pathAndQuery.IndexOf('?');
        if (queryStart < 0 || pathAndQuery[..queryStart] is not ("" or "/"))
        {
            return false;
        }

        var query = HttpUtility.ParseQueryString(uri.Query);
        if (query.Count != keys.Length)
        {
            return false;
        }

        foreach (var key in keys)
        {
            if (query.GetValues(key) is not { Length: 1 } values || string.IsNullOrWhiteSpace(values[0]))
            {
                return false;
            }
        }

        parameters = query;
        return true;
    }

    public static bool TryParseArchive(string? source,
        [NotNullWhen(true)] out Uri? archive, [NotNullWhen(true)] out string? entry)
    {
        archive = null;
        entry = null;
        if (!TryGetParameters(source, ARCHIVE_SCHEME, out var query, "source", "entry")
            || !Uri.TryCreate(query["source"], UriKind.Absolute, out var parsed) || !parsed.IsFile
            || parsed.Fragment.Length != 0 || parsed.UserInfo.Length != 0 || parsed.Port != -1)
        {
            return false;
        }

        archive = parsed;
        entry = query["entry"]!;
        return true;
    }

    public static bool TryParseThumbnail(string? source, [NotNullWhen(true)] out string? imageSource, out int width)
    {
        imageSource = null;
        width = 0;
        if (!TryGetParameters(source, THUMBNAIL_SCHEME, out var query, "source", "width")
            || !Uri.TryCreate(query["source"], UriKind.Absolute, out _)
            || !int.TryParse(query["width"], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedWidth)
            || parsedWidth is < 1 or > MAX_THUMBNAIL_WIDTH)
        {
            return false;
        }

        imageSource = query["source"]!;
        width = parsedWidth;
        return true;
    }

    public static bool TryParseSkin(string? source, out SkinViewType view, [NotNullWhen(true)] out string? skinSource)
    {
        view = default;
        skinSource = null;
        if (!TryGetParameters(source, SKIN_SCHEME, out var query, "type", "src")
            || !Enum.TryParse<SkinViewType>(query["type"], true, out var parsed) || !Enum.IsDefined(parsed)
            || !query["type"]!.Equals(parsed.ToString(), StringComparison.OrdinalIgnoreCase)
            || !SkinHelper.TryParseSource(query["src"], out _, out _))
        {
            return false;
        }

        view = parsed;
        skinSource = query["src"]!;
        return true;
    }

    public static bool TryParseData(string? source, [NotNullWhen(true)] out string? payload)
    {
        payload = null;
        if (!InternalUriHelper.HasScheme(source, DATA_SCHEME))
        {
            return false;
        }

        var comma = source!.IndexOf(',');
        if (comma < 0 || source.Contains('#') || !InternalUriHelper.HasValidEscaping(source))
        {
            return false;
        }

        var header = source[5..comma];
        if (!header.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || !header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase)
            || header.IndexOf(';') <= "image/".Length)
        {
            return false;
        }

        var decoded = Uri.UnescapeDataString(source[(comma + 1)..]);
        if (string.IsNullOrWhiteSpace(decoded))
        {
            return false;
        }

        payload = decoded;
        return true;
    }
}
