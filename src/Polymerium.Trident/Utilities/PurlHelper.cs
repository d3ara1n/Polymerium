using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Polymerium.Trident.Utilities;

public static class PurlHelper
{
    private static Regex PATTERN =
        new(
            "^(?<label>[a-zA-Z0-9._-]+):((?<namespace>[a-zA-Z0-9._-]+)/)?(?<identity>[a-zA-Z0-9._-]+)(@(?<version>[a-zA-Z0-9._-]+))?$");

    public static bool TryParse(string purl,
        out (string Label, string? Namespace, string Identity, string? Version) result)
    {
        var match = PATTERN.Match(purl);
        if (match.Success && match.Groups["label"].Success && match.Groups["identity"].Success)
        {
            result.Label = match.Groups["label"].Value;
            result.Namespace = match.Groups["namespace"].Success ? match.Groups["namespace"].Value : null;
            result.Identity = match.Groups["identity"].Value;
            result.Version = match.Groups["version"].Success ? match.Groups["version"].Value : null;

            return true;
        }

        result = default;
        return false;
    }
}