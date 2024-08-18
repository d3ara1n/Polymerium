using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Trident.Abstractions.Resources;

namespace Polymerium.Trident.Helpers;

public static class AurlHelper
{
    private static readonly Regex PATTERN =
        new("^(?<label>[a-z0-9_]+):(?<project>[a-zA-Z0-9_.]+)(\\/(?<version>[a-zA-Z0-9_.]+))?$");

    public static string MakeAurl(Attachment attachment) => attachment.VersionId != null
        ? $"{attachment.Label}:{attachment.ProjectId}/{attachment.VersionId}"
        : $"{attachment.Label}:{attachment.ProjectId}";

    public static bool TryParseAurl(string aurl, [MaybeNullWhen(false)] out Attachment result)
    {
        result = null;
        var match = PATTERN.Match(aurl);
        if (match.Success)
        {
            if (match.Groups.TryGetValue("label", out var label))
            {
                if (match.Groups.TryGetValue("project", out var project))
                {
                    if (match.Groups.TryGetValue("version", out var version))
                    {
                        result = new Attachment(label.Value, project.Value, version.Value);
                    }
                    else
                    {
                        result = new Attachment(label.Value, project.Value, null);
                    }

                    return true;
                }
            }
        }

        return false;
    }
}