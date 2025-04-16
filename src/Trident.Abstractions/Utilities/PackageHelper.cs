using System.Text;
using System.Text.RegularExpressions;
using Trident.Abstractions.Repositories;
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

    public static bool IsMatched(string left, string right) =>
        left == right || (TryParse(right, out var r) && IsMatched(left, r.Label, r.Namespace, r.Pid));

    public static bool IsMatched(string left, string label, string? ns, string pid) =>
        TryParse(left, out var l) && l.Label == label && l.Namespace == ns && l.Pid == pid;

    public static string ExtractProjectIdentityIfValid(string purl) =>
        TryParse(purl, out var result) ? ToPurl(result.Label, result.Namespace, result.Pid, null) : purl;

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

    public static string Identify(string label, string? ns, string pid, string? vid, Filter? filter)
    {
        var sb = new StringBuilder(label);
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
        else if (filter != null)
        {
            if (filter.Kind != null)
            {
                sb.Append("#kind=");
                sb.Append(filter.Kind);
            }

            if (filter.Version != null)
            {
                sb.Append("#version=");
                sb.Append(filter.Version);
            }

            if (filter.Loader != null)
            {
                sb.Append("#loader=");
                sb.Append(filter.Loader);
            }
        }

        return sb.ToString();
    }
}