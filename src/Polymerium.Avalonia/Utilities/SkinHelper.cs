using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Polymerium.Avalonia.Rendering;

namespace Polymerium.Avalonia.Utilities;

public static class SkinHelper
{
    public const string Scheme = "skin";

    public static string ToUri(SkinViewType view, string source) =>
        ImageSourceHelper.Create(Scheme, new NameValueCollection
        {
            ["type"] = view.ToString().ToLowerInvariant(),
            ["src"] = source
        }).AbsoluteUri;

    public static bool TryParse(string uri, out SkinViewType view, [NotNullWhen(true)] out string? source)
    {
        view = SkinViewType.Body;
        source = null;
        if (!ImageSourceHelper.TryGetParameters(uri, Scheme, out var query)
            || string.IsNullOrWhiteSpace(query["type"]) || string.IsNullOrWhiteSpace(query["src"]))
        {
            return false;
        }

        if (Enum.TryParse<SkinViewType>(query["type"], true, out var parsed) && Enum.IsDefined(parsed))
        {
            view = parsed;
        }

        source = query["src"]!;
        return true;
    }
}
