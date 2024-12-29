using System.Text;
using System.Text.RegularExpressions;
using Trident.Abstractions.Repositories.Resources;

namespace Trident.Abstractions.Utilities;

public static class PackageHelper
{
    private static readonly Regex PATTERN =
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

    public static string ToPurl(string label, string? @namespace, string identity, string? version)
    {
        var sb = new StringBuilder();
        sb.Append(label);
        sb.Append(':');
        if (@namespace != null)
        {
            sb.Append(@namespace);
            sb.Append('/');
        }

        sb.Append(identity);
        if (version != null)
        {
            sb.Append('@');
            sb.Append(version);
        }

        return sb.ToString();
    }

    public static string ToPurl(Package package)
    {
        return ToPurl(package.Label, package.Namespace, package.ProjectId, package.VersionId);
    }
}