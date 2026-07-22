using System;
using System.IO;
using Polymerium.Avalonia.Models;
using TridentCore.Pref.Parsing;

namespace Polymerium.Avalonia.Utilities;

// Classifies free-form importer input into a typed reference without any network access:
// local files become File, pref:// URIs parse straight into a PackageIdentifier (Pref), and
// http(s) URLs pass through as Uri for the consumer to resolve via RepositoryAgent.RecognizeAsync.
public static class ModpackUrlDetectionHelper
{
    public static ModpackImporterResult? Detect(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

        if (File.Exists(input))
        {
            return new ModpackImporterResult.File(input);
        }

        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            if (uri.Scheme.Equals("pref", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var descriptor = Parser.Default.Parse(input);
                    return new ModpackImporterResult.Pref(new(descriptor.Repository,
                                                              descriptor.Namespace,
                                                              descriptor.Identity,
                                                              descriptor.Version));
                }
                catch (FormatException)
                {
                    return null;
                }
            }

            if (uri.Scheme is "http" or "https")
            {
                return new ModpackImporterResult.Uri(uri);
            }
        }

        return null;
    }
}
