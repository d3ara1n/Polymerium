using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Web;

namespace Polymerium.Avalonia.Utilities;

public static class ImageSourceHelper
{
    public const string ARCHIVE_SCHEME = "archive";
    public const string THUMBNAIL_SCHEME = "thumbnail";
    public const string DATA_SCHEME = "data";
    private const int MAX_THUMBNAIL_WIDTH = 4096;

    public static Uri FromFile(string path) =>
        new UriBuilder(new Uri(Path.GetFullPath(path)))
        {
            Query = "version=" + File.GetLastWriteTimeUtc(path).Ticks.ToString(CultureInfo.InvariantCulture)
        }.Uri;

    public static Uri Archive(string path, string entry) =>
        Create(ARCHIVE_SCHEME, new() { ["source"] = FromFile(path).AbsoluteUri, ["entry"] = entry });

    public static Uri RelocateArchive(Uri source, string path) =>
        TryParseArchive(source.AbsoluteUri, out _, out var entry) ? Archive(path, entry) : source;

    public static Uri Thumbnail(Uri source, int width)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(width, MAX_THUMBNAIL_WIDTH);
        return Create(THUMBNAIL_SCHEME, new()
        {
            ["source"] = source.AbsoluteUri,
            ["width"] = width.ToString(CultureInfo.InvariantCulture)
        });
    }

    public static Uri FromBase64(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        payload = payload.Trim();
        return new(IsScheme(payload, DATA_SCHEME) ? payload : "data:image/png;base64," + payload);
    }

    public static bool IsScheme(string source, string scheme) =>
        source.StartsWith(scheme + ":", StringComparison.OrdinalIgnoreCase);

    public static Uri Create(string scheme, NameValueCollection parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query.Add(parameters);
        return new UriBuilder(scheme, "local") { Query = query.ToString() }.Uri;
    }

    public static bool TryGetParameters(string source, string scheme,
        [NotNullWhen(true)] out NameValueCollection? parameters)
    {
        parameters = null;
        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri)
            || !uri.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase)
            || uri.Host is not ("local" or "") || uri.AbsolutePath is not ("" or "/"))
        {
            return false;
        }

        parameters = HttpUtility.ParseQueryString(uri.Query);
        return true;
    }

    public static bool TryParseArchive(string source,
        [NotNullWhen(true)] out Uri? archive, [NotNullWhen(true)] out string? entry)
    {
        archive = null;
        entry = null;
        if (!TryGetParameters(source, ARCHIVE_SCHEME, out var query)
            || !Uri.TryCreate(query["source"], UriKind.Absolute, out var parsed) || !parsed.IsFile
            || string.IsNullOrEmpty(query["entry"]))
        {
            return false;
        }

        archive = parsed;
        entry = query["entry"]!;
        return true;
    }

    public static bool TryParseThumbnail(string source, [NotNullWhen(true)] out string? imageSource, out int width)
    {
        imageSource = null;
        width = 0;
        if (!TryGetParameters(source, THUMBNAIL_SCHEME, out var query)
            || string.IsNullOrWhiteSpace(query["source"])
            || !int.TryParse(query["width"], NumberStyles.None, CultureInfo.InvariantCulture, out width)
            || width is < 1 or > MAX_THUMBNAIL_WIDTH)
        {
            return false;
        }

        imageSource = query["source"]!;
        return true;
    }

    public static bool TryParseData(string source, [NotNullWhen(true)] out string? payload)
    {
        payload = null;
        if (!IsScheme(source, DATA_SCHEME))
        {
            return false;
        }

        var comma = source.IndexOf(',');
        if (comma < 0)
        {
            return false;
        }

        var header = source[5..comma];
        if (!header.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || !header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        payload = Uri.UnescapeDataString(source[(comma + 1)..]);
        return true;
    }
}
