using PackageUrl;

namespace Polymerium.Trident.Helpers
{
    public static class PurlHelper
    {
        public static bool TryParse(string purl, out (string type, string name, string? version)? result)
        {
            try
            {
                PackageURL parsed = new(purl);
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

        public static string MakePurl(string label, string projectId, string? versionId = null)
        {
            return new PackageURL(label, null, projectId, versionId, null,
                null).ToString();
        }
    }
}