using System.Text;
using System.Text.RegularExpressions;
using Trident.Abstractions.Repositories.Resources;

namespace Trident.Abstractions.Utilities;

public static class PackageHelper
{
    private static readonly Regex PATTERN =
        new("^(?<label>[a-zA-Z0-9._-]+):((?<namespace>[a-zA-Z0-9._-]+)/)?(?<identity>[a-zA-Z0-9._-]+)(@(?<version>[a-zA-Z0-9._-]+))?$");

    public static bool TryParse(string purl, out (string Label, string? Namespace, string Pid, string? Vid) result)
    {
        var match = PATTERN.Match(purl);
        if (match.Success && match.Groups["label"].Success && match.Groups["identity"].Success)
        {
            result.Label = match.Groups["label"].Value;
            result.Namespace = match.Groups["namespace"].Success ? match.Groups["namespace"].Value : null;
            result.Pid = match.Groups["identity"].Value;
            result.Vid = match.Groups["version"].Success ? match.Groups["version"].Value : null;

            return true;
        }

        result = default((string Label, string? Namespace, string Pid, string? Vid));
        return false;
    }

    public static string ToPurl(string label, string? ns, string pid, string? vid)
    {
        var sb = new StringBuilder();
        sb.Append(label);
        sb.Append(':');
        if (ns != null)
        {
            sb.Append(ns);
            sb.Append('/');
        }

        sb.Append(pid);
        if (vid != null)
        {
            sb.Append('@');
            sb.Append(vid);
        }

        return sb.ToString();
    }

    public static string ToPurl(Package package) =>
        ToPurl(package.Label, package.Namespace, package.ProjectId, package.VersionId);
}