using PackageUrl;

namespace Polymerium.App.Helpers;

public static class PurlHelper
{
    public static bool TryParse(string purl, out (string type, string name, string? version)? result)
    {
        try
        {
            var parsed = new PackageURL(purl);
            if (parsed is { Type: not null, Name: not null })
            {
                result = (parsed.Type, parsed.Name, parsed.Version);
                return true;
            }

            result = null;
        }
        catch (MalformedPackageUrlException)
        {
            result = null;
        }

        return false;
    }
}